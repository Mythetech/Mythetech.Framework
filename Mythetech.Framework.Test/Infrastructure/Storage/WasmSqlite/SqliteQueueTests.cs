using Mythetech.Framework.Infrastructure.Queue;
using Mythetech.Framework.WebAssembly.Storage.Sqlite;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Storage.WasmSqlite;

[Trait("Category", "WasmIntegration")]
public class SqliteQueueTests
{
    private const string TestConnectionString = "Data Source=queue_test.db";

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Enqueue_AddsItem_ReturnsId()
    {
        var queue = new SqliteQueue<TestMessage>(TestConnectionString, "queue_test");
        await queue.EnsureTableAsync();
        var message = new TestMessage { Content = "Hello" };

        var id = await queue.EnqueueAsync(message);

        id.ShouldNotBeNullOrEmpty();
        var pendingCount = await queue.GetPendingCountAsync();
        pendingCount.ShouldBe(1);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Dequeue_ReturnsOldest_MarksProcessing()
    {
        var queue = new SqliteQueue<TestMessage>(TestConnectionString, "queue_test");
        await queue.EnsureTableAsync();
        var id1 = await queue.EnqueueAsync(new TestMessage { Content = "First" });
        await Task.Delay(10);
        await queue.EnqueueAsync(new TestMessage { Content = "Second" });

        var entry = await queue.DequeueAsync();

        entry.ShouldNotBeNull();
        entry.Id.ShouldBe(id1);
        entry.Item.Content.ShouldBe("First");
        entry.Status.ShouldBe(QueueEntryStatus.Processing);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Dequeue_EmptyQueue_ReturnsNull()
    {
        var queue = new SqliteQueue<TestMessage>(TestConnectionString, "queue_empty");
        await queue.EnsureTableAsync();

        var entry = await queue.DequeueAsync();
        entry.ShouldBeNull();
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Peek_ReturnsItem_DoesNotRemove()
    {
        var queue = new SqliteQueue<TestMessage>(TestConnectionString, "queue_peek");
        await queue.EnsureTableAsync();
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });

        var entry1 = await queue.PeekAsync();
        var entry2 = await queue.PeekAsync();

        entry1.ShouldNotBeNull();
        entry2.ShouldNotBeNull();
        entry1.Id.ShouldBe(entry2.Id);
        entry1.Status.ShouldBe(QueueEntryStatus.Pending);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Complete_MarksEntryCompleted()
    {
        var queue = new SqliteQueue<TestMessage>(TestConnectionString, "queue_complete");
        await queue.EnsureTableAsync();
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });
        var entry = await queue.DequeueAsync();

        await queue.CompleteAsync(entry!.Id);

        var pendingCount = await queue.GetPendingCountAsync();
        pendingCount.ShouldBe(0);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Fail_MarksEntryFailed_WithReason()
    {
        var queue = new SqliteQueue<TestMessage>(TestConnectionString, "queue_fail");
        await queue.EnsureTableAsync();
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });
        var entry = await queue.DequeueAsync();

        await queue.FailAsync(entry!.Id, "Network error");

        var failed = await queue.GetFailedAsync();
        failed.Count.ShouldBe(1);
        failed[0].FailureReason.ShouldBe("Network error");
        failed[0].RetryCount.ShouldBe(1);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Retry_MovesFailedToPending()
    {
        var queue = new SqliteQueue<TestMessage>(TestConnectionString, "queue_retry");
        await queue.EnsureTableAsync();
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });
        var entry = await queue.DequeueAsync();
        await queue.FailAsync(entry!.Id, "First failure");

        await queue.RetryAsync(entry.Id);

        var pendingCount = await queue.GetPendingCountAsync();
        pendingCount.ShouldBe(1);
        var failedCount = await queue.GetFailedAsync();
        failedCount.Count.ShouldBe(0);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Retry_PreservesRetryCount()
    {
        var queue = new SqliteQueue<TestMessage>(TestConnectionString, "queue_retry_count");
        await queue.EnsureTableAsync();
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });

        var entry1 = await queue.DequeueAsync();
        await queue.FailAsync(entry1!.Id, "Failure 1");
        await queue.RetryAsync(entry1.Id);

        var entry2 = await queue.DequeueAsync();
        await queue.FailAsync(entry2!.Id, "Failure 2");

        var failed = await queue.GetFailedAsync();
        failed[0].RetryCount.ShouldBe(2);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Purge_RemovesOldCompleted()
    {
        var queue = new SqliteQueue<TestMessage>(TestConnectionString, "queue_purge");
        await queue.EnsureTableAsync();
        await queue.EnqueueAsync(new TestMessage { Content = "Old" });
        var entry = await queue.DequeueAsync();
        await queue.CompleteAsync(entry!.Id);

        await queue.EnqueueAsync(new TestMessage { Content = "New" });

        var removed = await queue.PurgeCompletedAsync(DateTime.UtcNow.AddMinutes(1));

        removed.ShouldBe(1);
        var pendingCount = await queue.GetPendingCountAsync();
        pendingCount.ShouldBe(1);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Factory_GetQueue_SameName_ReturnsSameInstance()
    {
        var factory = new SqliteQueueFactory("queue_test.db");
        var queue1 = await factory.GetQueueAsync<TestMessage>("test");
        var queue2 = await factory.GetQueueAsync<TestMessage>("test");

        queue1.ShouldBeSameAs(queue2);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Factory_GetQueue_DifferentNames_ReturnsDifferentInstances()
    {
        var factory = new SqliteQueueFactory("queue_test.db");
        var queue1 = await factory.GetQueueAsync<TestMessage>("queue1");
        var queue2 = await factory.GetQueueAsync<TestMessage>("queue2");

        queue1.ShouldNotBeSameAs(queue2);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task DifferentQueues_AreIsolated()
    {
        var factory = new SqliteQueueFactory("queue_test.db");
        var queueA = await factory.GetQueueAsync<TestMessage>("queue.a");
        var queueB = await factory.GetQueueAsync<TestMessage>("queue.b");

        await queueA!.EnqueueAsync(new TestMessage { Content = "A" });
        await queueB!.EnqueueAsync(new TestMessage { Content = "B1" });
        await queueB.EnqueueAsync(new TestMessage { Content = "B2" });

        var pendingA = await queueA.GetPendingCountAsync();
        var pendingB = await queueB.GetPendingCountAsync();

        pendingA.ShouldBe(1);
        pendingB.ShouldBe(2);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Factory_DeleteQueue_RemovesAll()
    {
        var factory = new SqliteQueueFactory("queue_test.db");
        var queue = await factory.GetQueueAsync<TestMessage>("to.delete");
        await queue!.EnqueueAsync(new TestMessage { Content = "Test" });

        var deleted = await factory.DeleteQueueAsync("to.delete");

        deleted.ShouldBeTrue();
        var names = await factory.GetQueueNamesAsync();
        names.ShouldNotContain("to.delete");
    }
}

public class TestMessage
{
    public string Content { get; set; } = string.Empty;
}
