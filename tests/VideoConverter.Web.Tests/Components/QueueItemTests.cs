using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using VideoConverter.Web.Components;
using VideoConverter.Web.Models;
using Xunit;

namespace VideoConverter.Web.Tests.Components;

public sealed class QueueItemTests : BunitContext
{
    public QueueItemTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Render_WithValidItem_DisplaysItemInformation()
    {
        // Arrange
        var item = new ConversionDto(
            1,
            "test-input.mp4",
            "test-output",
            DateTime.UtcNow,
            ConversionItemStatus.Pending,
            null,
            null,
            null,
            null,
            null);

        // Act
        var cut = Render<QueueItem>(parameters => parameters
            .Add(p => p.Item, item));

        // Assert
        cut.Markup.Should().Contain("test-input.mp4");
        cut.Markup.Should().Contain("test-output");
    }

    [Fact]
    public void Render_WhenDragging_AppliesDraggingClass()
    {
        // Arrange
        var item = CreateTestItem(1);

        // Act
        var cut = Render<QueueItem>(parameters => parameters
            .Add(p => p.Item, item)
            .Add(p => p.IsDragging, true));

        // Assert
        var element = cut.Find(".queue-item");
        element.ClassList.Should().Contain("queue-item-dragging");
    }

    [Fact]
    public void Render_WhenDragOverAbove_AppliesDragOverClasses()
    {
        // Arrange
        var item = CreateTestItem(1);

        // Act
        var cut = Render<QueueItem>(parameters => parameters
            .Add(p => p.Item, item)
            .Add(p => p.IsDragOver, true)
            .Add(p => p.DragOverPosition, "above"));

        // Assert
        var element = cut.Find(".queue-item");
        element.ClassList.Should().Contain("queue-item-drag-over");
        element.ClassList.Should().Contain("queue-item-drag-over-above");
    }

    [Fact]
    public void Render_WhenDragOverBelow_AppliesDragOverClasses()
    {
        // Arrange
        var item = CreateTestItem(1);

        // Act
        var cut = Render<QueueItem>(parameters => parameters
            .Add(p => p.Item, item)
            .Add(p => p.IsDragOver, true)
            .Add(p => p.DragOverPosition, "below"));

        // Assert
        var element = cut.Find(".queue-item");
        element.ClassList.Should().Contain("queue-item-drag-over");
        element.ClassList.Should().Contain("queue-item-drag-over-below");
    }

    [Fact]
    public async Task OnDragStart_WhenTriggered_InvokesCallback()
    {
        // Arrange
        var item = CreateTestItem(42);
        var dragStartInvoked = false;
        var draggedItemId = 0;

        // Act
        var cut = Render<QueueItem>(parameters => parameters
            .Add(p => p.Item, item)
            .Add(p => p.OnDragStart, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<int>(this, (id) =>
            {
                dragStartInvoked = true;
                draggedItemId = id;
            })));

        var element = cut.Find(".queue-item");
        await cut.InvokeAsync(() => element.DragStart(new DragEventArgs()));

        // Assert
        dragStartInvoked.Should().BeTrue();
        draggedItemId.Should().Be(42);
    }

    [Fact]
    public async Task OnDragEnd_WhenTriggered_InvokesCallback()
    {
        // Arrange
        var item = CreateTestItem(1);
        var dragEndInvoked = false;

        // Act
        var cut = Render<QueueItem>(parameters => parameters
            .Add(p => p.Item, item)
            .Add(p => p.OnDragEnd, Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () =>
            {
                dragEndInvoked = true;
            })));

        var element = cut.Find(".queue-item");
        await cut.InvokeAsync(() => element.DragEnd(new DragEventArgs()));

        // Assert
        dragEndInvoked.Should().BeTrue();
    }

    [Fact]
    public void Render_WithNormalState_HasCorrectAttributes()
    {
        // Arrange
        var item = CreateTestItem(1);

        // Act
        var cut = Render<QueueItem>(parameters => parameters
            .Add(p => p.Item, item));

        // Assert
        var element = cut.Find(".queue-item");
        element.GetAttribute("draggable").Should().Be("true");
        element.ClassList.Should().NotContain("queue-item-dragging");
        element.ClassList.Should().NotContain("queue-item-drag-over");
    }

    private static ConversionDto CreateTestItem(int id)
    {
        return new ConversionDto(
            id,
            $"input-{id}.mp4",
            $"output-{id}",
            DateTime.UtcNow,
            ConversionItemStatus.Pending,
            null,
            null,
            null,
            null,
            null);
    }
}
