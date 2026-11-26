using Mythetech.Framework.Infrastructure.Mcp;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp;

public class ClaudeCodeMcpCommandsTests
{
    [Fact(DisplayName = "Adding_removes_an_existing_entry_first_so_a_re_add_picks_up_the_token")]
    public void Adding_removes_an_existing_entry_first_so_a_re_add_picks_up_the_token()
    {
        var commands = ClaudeCodeMcpCommands.Add("horizon", "http://localhost:3334/mcp", "t0ken");

        commands.Count.ShouldBe(2);
        commands[0].ShouldBe(["mcp", "remove", "horizon"]);
        commands[1].ShouldBe(["mcp", "add", "--transport", "http", "horizon", "http://localhost:3334/mcp", "--header", "Authorization: Bearer t0ken"]);
    }

    [Fact(DisplayName = "Without_a_token_no_header_is_sent")]
    public void Without_a_token_no_header_is_sent()
    {
        ClaudeCodeMcpCommands.Add("horizon", "http://localhost:3334/mcp", null)[1]
            .ShouldBe(["mcp", "add", "--transport", "http", "horizon", "http://localhost:3334/mcp"]);
    }
}
