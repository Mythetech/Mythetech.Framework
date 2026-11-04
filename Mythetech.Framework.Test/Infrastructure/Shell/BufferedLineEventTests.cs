using Mythetech.Framework.Desktop.Services;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

public class BufferedLineEventTests
{
    [Fact(DisplayName = "Add_LinesRaisedBeforeTheFirstHandler_ReplayedToItInOrder")]
    public void Add_LinesRaisedBeforeTheFirstHandler_ReplayedToItInOrder()
    {
        var lines = new BufferedLineEvent(capacity: 10);
        var first = new List<string>();
        var second = new List<string>();

        lines.Raise("a");
        lines.Raise("b");
        lines.Add(first.Add);
        lines.Add(second.Add);
        lines.Raise("c");

        first.ShouldBe(["a", "b", "c"]);
        second.ShouldBe(["c"]);
    }

    [Fact(DisplayName = "Add_MoreLinesThanCapacityBeforeTheFirstHandler_KeepsTheFirstLines")]
    public void Add_MoreLinesThanCapacityBeforeTheFirstHandler_KeepsTheFirstLines()
    {
        var lines = new BufferedLineEvent(capacity: 2);
        var received = new List<string>();

        lines.Raise("a");
        lines.Raise("b");
        lines.Raise("c");
        lines.Add(received.Add);

        received.ShouldBe(["a", "b"]);
    }

    [Fact(DisplayName = "Remove_Handler_StopsReceivingLines")]
    public void Remove_Handler_StopsReceivingLines()
    {
        var lines = new BufferedLineEvent(capacity: 10);
        var received = new List<string>();

        lines.Add(received.Add);
        lines.Raise("a");
        lines.Remove(received.Add);
        lines.Raise("b");

        received.ShouldBe(["a"]);
    }
}
