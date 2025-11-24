using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using VideoConverter.Web.Components;
using VideoConverter.Web.Models;
using Xunit;

namespace VideoConverter.Web.Tests.Components;

public sealed class QueueListTests : BunitContext
{
    public QueueListTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void Render_WithEmptyList_ShowsEmptyMessage()
    {
        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, new List<ConversionDto>()));

        // Assert
        cut.Markup.Should().Contain("No pending conversions to reorder");
    }

    [Fact]
    public void Render_WithItems_DisplaysAllItems()
    {
        // Arrange
        var items = new List<ConversionDto>
        {
            CreateTestItem(1, "item1.mp4"),
            CreateTestItem(2, "item2.mp4"),
            CreateTestItem(3, "item3.mp4"),
        };

        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert
        cut.Markup.Should().Contain("item1.mp4");
        cut.Markup.Should().Contain("item2.mp4");
        cut.Markup.Should().Contain("item3.mp4");
    }

    [Fact]
    public async Task OnOrderChanged_WhenItemDropped_InvokesCallbackWithNewOrder()
    {
        // Arrange
        var items = new List<ConversionDto>
        {
            CreateTestItem(1, "item1.mp4"),
            CreateTestItem(2, "item2.mp4"),
            CreateTestItem(3, "item3.mp4"),
        };

        List<ConversionDto>? newOrder = null;

        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.OnOrderChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<List<ConversionDto>>(this, (order) =>
            {
                newOrder = order;
            })));

        var queueItems = cut.FindAll(".queue-item");
        await cut.InvokeAsync(() => queueItems[0].DragStart(new DragEventArgs()));
        await cut.InvokeAsync(() => queueItems[1].Drop(new DragEventArgs()));

        // Assert
        newOrder.Should().NotBeNull();
        newOrder!.Count.Should().Be(3);
        newOrder[0].Id.Should().Be(2);
        newOrder[1].Id.Should().Be(1);
        newOrder[2].Id.Should().Be(3);
    }

    [Fact]
    public async Task HandleDragStart_WhenCalled_SetsDraggingState()
    {
        // Arrange
        var items = new List<ConversionDto>
        {
            CreateTestItem(1, "item1.mp4"),
            CreateTestItem(2, "item2.mp4"),
        };

        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, items));

        var queueItems = cut.FindAll(".queue-item");
        await cut.InvokeAsync(() => queueItems[0].DragStart(new DragEventArgs()));

        // Assert
        cut.Render();
        var renderedItems = cut.FindAll(".queue-item");
        renderedItems[0].ClassList.Should().Contain("queue-item-dragging");
        renderedItems[1].ClassList.Should().NotContain("queue-item-dragging");
    }

    [Fact]
    public async Task HandleDragEnd_WhenCalled_ClearsDraggingState()
    {
        // Arrange
        var items = new List<ConversionDto>
        {
            CreateTestItem(1, "item1.mp4"),
            CreateTestItem(2, "item2.mp4"),
        };

        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, items));

        var queueItems = cut.FindAll(".queue-item");
        await cut.InvokeAsync(() => queueItems[0].DragStart(new DragEventArgs()));
        await cut.InvokeAsync(() => queueItems[0].DragEnd(new DragEventArgs()));

        // Assert
        cut.Render();
        var renderedItems = cut.FindAll(".queue-item");
        renderedItems[0].ClassList.Should().NotContain("queue-item-dragging");
    }

    [Fact]
    public async Task HandleDrop_WhenDroppingOnSameItem_DoesNotChangeOrder()
    {
        // Arrange
        var items = new List<ConversionDto>
        {
            CreateTestItem(1, "item1.mp4"),
            CreateTestItem(2, "item2.mp4"),
        };

        List<ConversionDto>? newOrder = null;

        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.OnOrderChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<List<ConversionDto>>(this, (order) =>
            {
                newOrder = order;
            })));

        var queueItems = cut.FindAll(".queue-item");
        await cut.InvokeAsync(() => queueItems[0].DragStart(new DragEventArgs()));
        await cut.InvokeAsync(() => queueItems[0].Drop(new DragEventArgs()));

        // Assert
        newOrder.Should().BeNull();
    }

    [Fact]
    public async Task HandleDrop_WhenMovingItemDown_InsertsAtCorrectPosition()
    {
        // Arrange
        var items = new List<ConversionDto>
        {
            CreateTestItem(1, "item1.mp4"),
            CreateTestItem(2, "item2.mp4"),
            CreateTestItem(3, "item3.mp4"),
        };

        List<ConversionDto>? newOrder = null;

        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.OnOrderChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<List<ConversionDto>>(this, (order) =>
            {
                newOrder = order;
            })));

        var queueItems = cut.FindAll(".queue-item");
        await cut.InvokeAsync(() => queueItems[0].DragStart(new DragEventArgs()));
        await cut.InvokeAsync(() => queueItems[2].Drop(new DragEventArgs()));

        // Assert
        newOrder.Should().NotBeNull();
        newOrder!.Select(i => i.Id).Should().ContainInOrder(2, 3, 1);
    }

    [Fact]
    public async Task HandleDrop_WhenMovingItemUp_InsertsAtCorrectPosition()
    {
        // Arrange
        var items = new List<ConversionDto>
        {
            CreateTestItem(1, "item1.mp4"),
            CreateTestItem(2, "item2.mp4"),
            CreateTestItem(3, "item3.mp4"),
        };

        List<ConversionDto>? newOrder = null;

        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.OnOrderChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<List<ConversionDto>>(this, (order) =>
            {
                newOrder = order;
            })));

        var queueItems = cut.FindAll(".queue-item");
        await cut.InvokeAsync(() => queueItems[2].DragStart(new DragEventArgs()));
        await cut.InvokeAsync(() => queueItems[0].Drop(new DragEventArgs()));

        // Assert
        newOrder.Should().NotBeNull();
        newOrder!.Select(i => i.Id).Should().ContainInOrder(3, 1, 2);
    }

    [Fact]
    public async Task HandleDropOutside_WhenCalled_MovesItemToEnd()
    {
        // Arrange
        var items = new List<ConversionDto>
        {
            CreateTestItem(1, "item1.mp4"),
            CreateTestItem(2, "item2.mp4"),
            CreateTestItem(3, "item3.mp4"),
        };

        List<ConversionDto>? newOrder = null;

        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.OnOrderChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<List<ConversionDto>>(this, (order) =>
            {
                newOrder = order;
            })));

        var queueItems = cut.FindAll(".queue-item");
        var listContainer = cut.Find(".queue-list");
        await cut.InvokeAsync(() => queueItems[0].DragStart(new DragEventArgs()));
        await cut.InvokeAsync(() => listContainer.Drop(new DragEventArgs()));

        // Assert
        newOrder.Should().NotBeNull();
        newOrder!.Select(i => i.Id).Should().ContainInOrder(2, 3, 1);
    }

    [Fact]
    public async Task Render_WhenDragging_ShowsDropIndicator()
    {
        // Arrange
        var items = new List<ConversionDto>
        {
            CreateTestItem(1, "item1.mp4"),
            CreateTestItem(2, "item2.mp4"),
        };

        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, items));

        var queueItems = cut.FindAll(".queue-item");
        await cut.InvokeAsync(() => queueItems[0].DragStart(new DragEventArgs()));
        await cut.InvokeAsync(() => queueItems[1].DragOver(new DragEventArgs()));

        // Assert
        cut.Render();
        cut.Markup.Should().Contain("queue-drop-indicator");
    }

    [Fact]
    public async Task HandleDrop_WithSingleItem_DoesNotChangeOrder()
    {
        // Arrange
        var items = new List<ConversionDto>
        {
            CreateTestItem(1, "item1.mp4"),
        };

        List<ConversionDto>? newOrder = null;

        // Act
        var cut = Render<QueueList>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.OnOrderChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<List<ConversionDto>>(this, (order) =>
            {
                newOrder = order;
            })));

        var queueItems = cut.FindAll(".queue-item");
        await cut.InvokeAsync(() => queueItems[0].DragStart(new DragEventArgs()));
        await cut.InvokeAsync(() => queueItems[0].Drop(new DragEventArgs()));

        // Assert
        newOrder.Should().BeNull();
    }

    private static ConversionDto CreateTestItem(int id, string inputPath)
    {
        return new ConversionDto(
            id,
            inputPath,
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
