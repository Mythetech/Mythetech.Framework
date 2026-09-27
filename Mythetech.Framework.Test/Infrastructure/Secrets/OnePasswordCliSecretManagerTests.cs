using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Desktop.Secrets;
using Mythetech.Framework.Infrastructure.Secrets;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Secrets;

/// <summary>
/// Drives <see cref="OnePasswordCliSecretManager"/> against a fake runner, so no test ever starts the real
/// 1Password CLI or touches a real vault.
/// </summary>
public class OnePasswordCliSecretManagerTests
{
    private const string Key = "Aion connection 1";

    private const string NotAnItemError =
        "[ERROR] 2026/09/26 12:00:00 \"Aion connection 1\" isn't an item. Specify the item with its UUID, name, or domain.\n";

    private const string ExistingItemJson = """
        {
          "id": "abc123",
          "title": "Aion connection 1",
          "version": 2,
          "vault": { "id": "vault1", "name": "Private" },
          "category": "PASSWORD",
          "fields": [
            {
              "id": "password",
              "type": "CONCEALED",
              "purpose": "PASSWORD",
              "label": "password",
              "value": "old-value",
              "reference": "op://Private/Aion connection 1/password"
            },
            {
              "id": "notesPlain",
              "type": "STRING",
              "purpose": "NOTES",
              "label": "notesPlain",
              "value": "keep me",
              "reference": "op://Private/Aion connection 1/notesPlain"
            }
          ]
        }
        """;

    private readonly FakeOnePasswordCliRunner _runner = new();
    private readonly OnePasswordCliSecretManager _manager;

    public OnePasswordCliSecretManagerTests()
    {
        _manager = new OnePasswordCliSecretManager(NullLogger<OnePasswordCliSecretManager>.Instance, _runner);
    }

    #region Reads

    [Fact(DisplayName = "GetSecretAsync reads the password field from 'op item get' without piping stdin")]
    public async Task GetSecretAsync_ReadsPasswordField()
    {
        _runner.On("item get", Succeeded(ExistingItemJson));

        var result = await _manager.GetSecretAsync(Key, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        result.Value!.Value.ShouldBe("old-value");
        result.Value.Key.ShouldBe("abc123");
        result.Value.Name.ShouldBe(Key);
        _runner.Calls.Single().Arguments.ShouldBe(["item", "get", Key, "--format", "json"]);
        _runner.Calls.Single().StandardInput.ShouldBeNull();
    }

    [Fact(DisplayName = "GetSecretAsync maps a missing item to NotFound")]
    public async Task GetSecretAsync_MissingItem_ReturnsNotFound()
    {
        _runner.On("item get", Failed(NotAnItemError));

        var result = await _manager.GetSecretAsync(Key, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.NotFound);
    }

    [Fact(DisplayName = "GetSecretAsync maps 'not signed in' to AccessDenied")]
    public async Task GetSecretAsync_NotSignedIn_ReturnsAccessDenied()
    {
        _runner.On("item get", Failed("[ERROR] 2026/09/26 12:00:00 account is not signed in\n"));

        var result = await _manager.GetSecretAsync(Key, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.AccessDenied);
    }

    [Fact(DisplayName = "ListSecretsAsync parses the output of 'op item list'")]
    public async Task ListSecretsAsync_ParsesItemList()
    {
        _runner.On("item list", Succeeded("""[{ "id": "a1", "title": "Alpha", "category": "PASSWORD", "tags": ["db"] }]"""));

        var result = await _manager.ListSecretsAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        var secret = result.Value!.Single();
        secret.Key.ShouldBe("a1");
        secret.Name.ShouldBe("Alpha");
        secret.Category.ShouldBe("PASSWORD");
        secret.Tags.ShouldBe(["db"]);
        _runner.Calls.Single().Arguments.ShouldBe(["item", "list", "--format", "json"]);
    }

    [Fact(DisplayName = "TestConnectionAsync succeeds when 'op account list' lists an account")]
    public async Task TestConnectionAsync_AccountListed_ReturnsOk()
    {
        _runner.On("account list", Succeeded("URL  EMAIL  USER ID\nmy.1password.com  me@example.com  ABC"));

        var result = await _manager.TestConnectionAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        _runner.Calls.Single().Arguments.ShouldBe(["account", "list"]);
    }

    #endregion

    #region SetSecretAsync

    [Fact(DisplayName = "The 1Password CLI manager can write secrets")]
    public void Manager_IsSecretWriter()
    {
        new SecretManagerState().CanWrite(_manager).ShouldBeTrue();
    }

    [Fact(DisplayName = "SetSecretAsync creates a Password item from a template piped to 'op item create -' when none exists")]
    public async Task SetSecretAsync_NoExistingItem_CreatesPasswordItemFromStdin()
    {
        _runner.On("item get", Failed(NotAnItemError));
        _runner.On("item create", Succeeded());

        var result = await _manager.SetSecretAsync(Key, "new-value", TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        _runner.Calls.Count.ShouldBe(2);
        _runner.Calls[0].Arguments.ShouldBe(["item", "get", Key, "--format", "json"]);
        _runner.Calls[1].Arguments.ShouldBe(["item", "create", "-"]);

        using var template = JsonDocument.Parse(_runner.Calls[1].StandardInput!);
        template.RootElement.GetProperty("title").GetString().ShouldBe(Key);
        template.RootElement.GetProperty("category").GetString().ShouldBe("PASSWORD");
        var password = PasswordField(template.RootElement);
        password.GetProperty("type").GetString().ShouldBe("CONCEALED");
        password.GetProperty("purpose").GetString().ShouldBe("PASSWORD");
        password.GetProperty("value").GetString().ShouldBe("new-value");
    }

    [Fact(DisplayName = "SetSecretAsync updates an existing item by piping its edited JSON to 'op item edit <id>'")]
    public async Task SetSecretAsync_ExistingItem_EditsPasswordFieldFromStdin()
    {
        _runner.On("item get", Succeeded(ExistingItemJson));
        _runner.On("item edit", Succeeded());

        var result = await _manager.SetSecretAsync(Key, "new-value", TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        _runner.Calls.Count.ShouldBe(2);
        _runner.Calls[1].Arguments.ShouldBe(["item", "edit", "abc123"]);

        using var edited = JsonDocument.Parse(_runner.Calls[1].StandardInput!);
        edited.RootElement.GetProperty("id").GetString().ShouldBe("abc123");
        edited.RootElement.GetProperty("title").GetString().ShouldBe(Key);
        PasswordField(edited.RootElement).GetProperty("value").GetString().ShouldBe("new-value");
        var notes = edited.RootElement.GetProperty("fields").EnumerateArray()
            .Single(field => field.GetProperty("id").GetString() == "notesPlain");
        notes.GetProperty("value").GetString().ShouldBe("keep me");
    }

    [Theory(DisplayName = "SetSecretAsync never puts the secret value in the op arguments")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetSecretAsync_ValueNeverInArguments(bool itemExists)
    {
        const string secret = "s3cr3t \"quoted\" ☃ ; rm -rf ~";
        _runner.On("item get", itemExists ? Succeeded(ExistingItemJson) : Failed(NotAnItemError));
        _runner.On("item create", Succeeded());
        _runner.On("item edit", Succeeded());

        var result = await _manager.SetSecretAsync(Key, secret, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        _runner.Calls.SelectMany(call => call.Arguments).ShouldAllBe(argument => !argument.Contains("s3cr3t"));
        _runner.Calls.Last().StandardInput.ShouldNotBeNull();
        _runner.Calls.Last().StandardInput!.ShouldContain("s3cr3t");
    }

    [Theory(DisplayName = "A stored value reads back unchanged through GetSecretAsync")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetSecretAsync_StoredValue_ReadsBackThroughGetSecretAsync(bool itemExists)
    {
        const string secret = "p@ss \"word\" \\ ☃ {json} \n second line";
        _runner.On("item get", itemExists ? Succeeded(ExistingItemJson) : Failed(NotAnItemError));
        _runner.On("item create", Succeeded());
        _runner.On("item edit", Succeeded());
        await _manager.SetSecretAsync(Key, secret, TestContext.Current.CancellationToken);

        // Stand in for 1Password: what op stores is the piped item, which 'op item get' then returns with an id.
        var stored = JsonNode.Parse(_runner.Calls.Last().StandardInput!)!.AsObject();
        stored["id"] ??= "created1";
        _runner.On("item get", Succeeded(stored.ToJsonString()));

        var result = await _manager.GetSecretAsync(Key, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        result.Value!.Value.ShouldBe(secret);
    }

    [Theory(DisplayName = "SetSecretAsync maps signed-out and locked errors to AccessDenied without writing")]
    [InlineData("[ERROR] 2026/09/26 12:00:00 You are not currently signed in. Please run `op signin --help` for instructions")]
    [InlineData("[ERROR] 2026/09/26 12:00:00 account is not signed in")]
    [InlineData("[ERROR] 2026/09/26 12:00:00 1Password app is locked. Please open 1Password, unlock it with your password, and then try again")]
    [InlineData("[ERROR] 2026/09/26 12:00:00 authorization prompt dismissed, please try again")]
    [InlineData("[ERROR] 2026/09/26 12:00:00 authorization timeout")]
    [InlineData("[ERROR] 2026/09/26 12:00:00 session expired, sign in to create a new session")]
    public async Task SetSecretAsync_NotSignedInOrLocked_ReturnsAccessDenied(string error)
    {
        _runner.On("item get", Failed(error));

        var result = await _manager.SetSecretAsync(Key, "new-value", TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.AccessDenied);
        _runner.Calls.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "SetSecretAsync maps a locked 1Password during create to AccessDenied")]
    public async Task SetSecretAsync_LockedDuringCreate_ReturnsAccessDenied()
    {
        _runner.On("item get", Failed(NotAnItemError));
        _runner.On("item create", Failed("[ERROR] 2026/09/26 12:00:00 1Password app is locked. Please open 1Password, unlock it with your password, and then try again"));

        var result = await _manager.SetSecretAsync(Key, "new-value", TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.AccessDenied);
    }

    [Fact(DisplayName = "SetSecretAsync reports an edit failure with op's error and no secret")]
    public async Task SetSecretAsync_EditFails_ReturnsUnknownWithoutSecret()
    {
        _runner.On("item get", Succeeded(ExistingItemJson));
        _runner.On("item edit", Failed("[ERROR] 2026/09/26 12:00:00 You don't have permission to edit items in this vault"));

        var result = await _manager.SetSecretAsync(Key, "new-value", TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.Unknown);
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("permission");
        result.ErrorMessage.ShouldNotContain("new-value");
    }

    [Fact(DisplayName = "SetSecretAsync refuses to overwrite an item that has no password field")]
    public async Task SetSecretAsync_ExistingItemWithoutPasswordField_DoesNotEdit()
    {
        _runner.On("item get", Succeeded("""
            { "id": "note1", "title": "Aion connection 1", "category": "SECURE_NOTE",
              "fields": [ { "id": "notesPlain", "type": "STRING", "purpose": "NOTES", "label": "notesPlain", "value": "hi" } ] }
            """));

        var result = await _manager.SetSecretAsync(Key, "new-value", TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.InvalidKey);
        _runner.Calls.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "SetSecretAsync reports ConnectionFailed when op cannot be started")]
    public async Task SetSecretAsync_OpNotInstalled_ReturnsConnectionFailed()
    {
        _runner.Throws("item get", new Win32Exception(2, "No such file or directory"));

        var result = await _manager.SetSecretAsync(Key, "new-value", TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.ConnectionFailed);
    }

    [Theory(DisplayName = "SetSecretAsync rejects an empty key without running op")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetSecretAsync_EmptyKey_ReturnsInvalidKey(string key)
    {
        var result = await _manager.SetSecretAsync(key, "new-value", TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.InvalidKey);
        _runner.Calls.ShouldBeEmpty();
    }

    #endregion

    #region DeleteSecretAsync

    [Fact(DisplayName = "DeleteSecretAsync runs 'op item delete <key>' without stdin")]
    public async Task DeleteSecretAsync_DeletesItem()
    {
        _runner.On("item delete", Succeeded());

        var result = await _manager.DeleteSecretAsync(Key, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        _runner.Calls.Single().Arguments.ShouldBe(["item", "delete", Key]);
        _runner.Calls.Single().StandardInput.ShouldBeNull();
    }

    [Fact(DisplayName = "DeleteSecretAsync maps a missing item to NotFound")]
    public async Task DeleteSecretAsync_MissingItem_ReturnsNotFound()
    {
        _runner.On("item delete", Failed(NotAnItemError));

        var result = await _manager.DeleteSecretAsync(Key, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.NotFound);
    }

    [Fact(DisplayName = "DeleteSecretAsync maps a locked 1Password to AccessDenied")]
    public async Task DeleteSecretAsync_Locked_ReturnsAccessDenied()
    {
        _runner.On("item delete", Failed("[ERROR] 2026/09/26 12:00:00 1Password app is locked. Please open 1Password, unlock it with your password, and then try again"));

        var result = await _manager.DeleteSecretAsync(Key, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.AccessDenied);
    }

    [Fact(DisplayName = "DeleteSecretAsync rejects an empty key without running op")]
    public async Task DeleteSecretAsync_EmptyKey_ReturnsInvalidKey()
    {
        var result = await _manager.DeleteSecretAsync(" ", TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.ErrorKind.ShouldBe(SecretOperationErrorKind.InvalidKey);
        _runner.Calls.ShouldBeEmpty();
    }

    #endregion

    private static JsonElement PasswordField(JsonElement item) =>
        item.GetProperty("fields").EnumerateArray().Single(field => field.GetProperty("id").GetString() == "password");

    private static OnePasswordCliResult Succeeded(string standardOutput = "") => new(0, standardOutput, string.Empty);

    private static OnePasswordCliResult Failed(string standardError) => new(1, string.Empty, standardError);

    /// <summary>
    /// Answers each 'op' call by its first two arguments (for example "item get") and records every call,
    /// so tests can assert on exactly what reached argv and stdin.
    /// </summary>
    private sealed class FakeOnePasswordCliRunner : IOnePasswordCliRunner
    {
        private readonly Dictionary<string, Func<OnePasswordCliResult>> _responses = new();

        public List<(IReadOnlyList<string> Arguments, string? StandardInput)> Calls { get; } = [];

        public void On(string command, OnePasswordCliResult result) => _responses[command] = () => result;

        public void Throws(string command, Exception exception) => _responses[command] = () => throw exception;

        public Task<OnePasswordCliResult> RunAsync(
            IReadOnlyList<string> arguments,
            string? standardInput,
            CancellationToken cancellationToken)
        {
            Calls.Add((arguments.ToArray(), standardInput));

            var command = string.Join(' ', arguments.Take(2));
            if (!_responses.TryGetValue(command, out var respond))
            {
                throw new InvalidOperationException($"Unexpected op call: {command}");
            }

            return Task.FromResult(respond());
        }
    }
}
