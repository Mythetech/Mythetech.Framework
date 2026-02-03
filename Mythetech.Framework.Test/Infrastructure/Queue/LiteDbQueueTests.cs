using Mythetech.Framework.Desktop.Queue;
using Mythetech.Framework.Infrastructure.Queue;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Queue;

public class LiteDbQueueTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly LiteDbQueueFactory _factory;

    public LiteDbQueueTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"queue_test_{Guid.NewGuid()}.db");
        _factory = new LiteDbQueueFactory(_testDbPath);
    }

    #region Basic Operations Tests

    [Fact(DisplayName = "EnqueueAsync adds item to queue and returns ID")]
    public async Task Enqueue_AddsItem_ReturnsId()
    {
        // Arrange
        var queue = _factory.GetQueue<TestMessage>("test")!;
        var message = new TestMessage { Content = "Hello" };

        // Act
        var id = await queue.EnqueueAsync(message);

        // Assert
        id.ShouldNotBeNullOrEmpty();
        var pendingCount = await queue.GetPendingCountAsync();
        pendingCount.ShouldBe(1);
    }

    [Fact(DisplayName = "DequeueAsync returns oldest pending item and marks as processing")]
    public async Task Dequeue_ReturnsOldest_MarksProcessing()
    {
        // Arrange
        var queue = _factory.GetQueue<TestMessage>("test")!;
        var id1 = await queue.EnqueueAsync(new TestMessage { Content = "First" });
        await Task.Delay(10); // Ensure different timestamps
        var id2 = await queue.EnqueueAsync(new TestMessage { Content = "Second" });

        // Act
        var entry = await queue.DequeueAsync();

        // Assert
        entry.ShouldNotBeNull();
        entry.Id.ShouldBe(id1);
        entry.Item.Content.ShouldBe("First");
        entry.Status.ShouldBe(QueueEntryStatus.Processing);

        // Pending count should decrease
        var pendingCount = await queue.GetPendingCountAsync();
        pendingCount.ShouldBe(1);
    }

    [Fact(DisplayName = "DequeueAsync returns null when queue is empty")]
    public async Task Dequeue_EmptyQueue_ReturnsNull()
    {
        // Arrange
        var queue = _factory.GetQueue<TestMessage>("test")!;

        // Act
        var entry = await queue.DequeueAsync();

        // Assert
        entry.ShouldBeNull();
    }

    [Fact(DisplayName = "PeekAsync returns next item without removing")]
    public async Task Peek_ReturnsItem_DoesNotRemove()
    {
        // Arrange
        var queue = _factory.GetQueue<TestMessage>("test")!;
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });

        // Act
        var entry1 = await queue.PeekAsync();
        var entry2 = await queue.PeekAsync();

        // Assert
        entry1.ShouldNotBeNull();
        entry2.ShouldNotBeNull();
        entry1.Id.ShouldBe(entry2.Id);
        entry1.Status.ShouldBe(QueueEntryStatus.Pending);
    }

    [Fact(DisplayName = "CompleteAsync marks entry as completed")]
    public async Task Complete_MarksEntryCompleted()
    {
        // Arrange
        var queue = _factory.GetQueue<TestMessage>("test")!;
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });
        var entry = await queue.DequeueAsync();

        // Act
        await queue.CompleteAsync(entry!.Id);

        // Assert
        var pendingCount = await queue.GetPendingCountAsync();
        pendingCount.ShouldBe(0);
    }

    [Fact(DisplayName = "FailAsync marks entry as failed with reason")]
    public async Task Fail_MarksEntryFailed_WithReason()
    {
        // Arrange
        var queue = _factory.GetQueue<TestMessage>("test")!;
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });
        var entry = await queue.DequeueAsync();

        // Act
        await queue.FailAsync(entry!.Id, "Network error");

        // Assert
        var failed = await queue.GetFailedAsync();
        failed.Count.ShouldBe(1);
        failed[0].FailureReason.ShouldBe("Network error");
        failed[0].RetryCount.ShouldBe(1);
    }

    #endregion

    #region Retry Tests

    [Fact(DisplayName = "RetryAsync moves failed entry back to pending")]
    public async Task Retry_MovesFailedToPending()
    {
        // Arrange
        var queue = _factory.GetQueue<TestMessage>("test")!;
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });
        var entry = await queue.DequeueAsync();
        await queue.FailAsync(entry!.Id, "First failure");

        // Act
        await queue.RetryAsync(entry.Id);

        // Assert
        var pendingCount = await queue.GetPendingCountAsync();
        pendingCount.ShouldBe(1);

        var failedCount = await queue.GetFailedAsync();
        failedCount.Count.ShouldBe(0);
    }

    [Fact(DisplayName = "RetryAsync preserves retry count")]
    public async Task Retry_PreservesRetryCount()
    {
        // Arrange
        var queue = _factory.GetQueue<TestMessage>("test")!;
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });

        // Fail twice
        var entry1 = await queue.DequeueAsync();
        await queue.FailAsync(entry1!.Id, "Failure 1");
        await queue.RetryAsync(entry1.Id);

        var entry2 = await queue.DequeueAsync();
        await queue.FailAsync(entry2!.Id, "Failure 2");

        // Act
        var failed = await queue.GetFailedAsync();

        // Assert
        failed[0].RetryCount.ShouldBe(2);
    }

    #endregion

    #region Purge Tests

    [Fact(DisplayName = "PurgeCompletedAsync removes old completed entries")]
    public async Task Purge_RemovesOldCompleted()
    {
        // Arrange
        var queue = _factory.GetQueue<TestMessage>("test")!;
        await queue.EnqueueAsync(new TestMessage { Content = "Old" });
        var entry = await queue.DequeueAsync();
        await queue.CompleteAsync(entry!.Id);

        // Add a new item
        await queue.EnqueueAsync(new TestMessage { Content = "New" });

        // Act
        var removed = await queue.PurgeCompletedAsync(DateTime.UtcNow.AddMinutes(1));

        // Assert
        removed.ShouldBe(1);
        var pendingCount = await queue.GetPendingCountAsync();
        pendingCount.ShouldBe(1);
    }

    #endregion

    #region Factory Tests

    [Fact(DisplayName = "GetQueue returns same instance for same name")]
    public void GetQueue_SameName_ReturnsSameInstance()
    {
        // Act
        var queue1 = _factory.GetQueue<TestMessage>("test");
        var queue2 = _factory.GetQueue<TestMessage>("test");

        // Assert
        queue1.ShouldBeSameAs(queue2);
    }

    [Fact(DisplayName = "GetQueue returns different instances for different names")]
    public void GetQueue_DifferentNames_ReturnsDifferentInstances()
    {
        // Act
        var queue1 = _factory.GetQueue<TestMessage>("queue1");
        var queue2 = _factory.GetQueue<TestMessage>("queue2");

        // Assert
        queue1.ShouldNotBeSameAs(queue2);
    }

    [Fact(DisplayName = "Different queues are isolated")]
    public async Task DifferentQueues_AreIsolated()
    {
        // Arrange
        var queueA = _factory.GetQueue<TestMessage>("queue.a")!;
        var queueB = _factory.GetQueue<TestMessage>("queue.b")!;

        // Act
        await queueA.EnqueueAsync(new TestMessage { Content = "A" });
        await queueB.EnqueueAsync(new TestMessage { Content = "B1" });
        await queueB.EnqueueAsync(new TestMessage { Content = "B2" });

        // Assert
        var pendingA = await queueA.GetPendingCountAsync();
        var pendingB = await queueB.GetPendingCountAsync();

        pendingA.ShouldBe(1);
        pendingB.ShouldBe(2);
    }

    [Fact(DisplayName = "GetQueueNames returns all created queues")]
    public async Task GetQueueNames_ReturnsAllQueues()
    {
        // Arrange
        var queue1 = _factory.GetQueue<TestMessage>("reports")!;
        var queue2 = _factory.GetQueue<TestMessage>("notifications")!;
        await queue1.EnqueueAsync(new TestMessage { Content = "Test" });
        await queue2.EnqueueAsync(new TestMessage { Content = "Test" });

        // Act
        var names = _factory.GetQueueNames().ToList();

        // Assert
        names.ShouldContain("reports");
        names.ShouldContain("notifications");
    }

    [Fact(DisplayName = "DeleteQueueAsync removes queue and all entries")]
    public async Task DeleteQueue_RemovesAll()
    {
        // Arrange
        var queue = _factory.GetQueue<TestMessage>("to.delete")!;
        await queue.EnqueueAsync(new TestMessage { Content = "Test" });

        // Act
        var deleted = await _factory.DeleteQueueAsync("to.delete");

        // Assert
        deleted.ShouldBeTrue();
        var names = _factory.GetQueueNames().ToList();
        names.ShouldNotContain("to.delete");
    }

    #endregion

    #region Complex Type Tests

    [Fact(DisplayName = "Complex objects are serialized and deserialized correctly")]
    public async Task ComplexObjects_SerializeCorrectly()
    {
        // Arrange
        var queue = _factory.GetQueue<ComplexMessage>("test")!;
        var message = new ComplexMessage
        {
            Id = Guid.NewGuid(),
            Title = "Test Report",
            Data = new Dictionary<string, object>
            {
                ["count"] = 42,
                ["enabled"] = true
            },
            Tags = ["important", "urgent"],
            Timestamp = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        await queue.EnqueueAsync(message);
        var entry = await queue.DequeueAsync();

        // Assert
        entry.ShouldNotBeNull();
        entry.Item.Id.ShouldBe(message.Id);
        entry.Item.Title.ShouldBe("Test Report");
        entry.Item.Tags.ShouldContain("important");
        entry.Item.Tags.ShouldContain("urgent");
        entry.Item.Timestamp.ShouldBe(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    #endregion

    public void Dispose()
    {
        _factory.Dispose();

        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }

        GC.SuppressFinalize(this);
    }
}

#region Test Models

public class TestMessage
{
    public string Content { get; set; } = string.Empty;
}

public class ComplexMessage
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> Tags { get; set; } = [];
    public DateTime Timestamp { get; set; }
}

#endregion
