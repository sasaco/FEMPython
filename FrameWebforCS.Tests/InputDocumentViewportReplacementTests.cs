using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using FrameWebforCS.three;
using FrameWebforCS.components.result;
using SingleFormsDemo;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class InputDocumentViewportReplacementTests
{
    [Fact]
    public void LateInvalidResultPreservesInputAndViewportDocument()
    {
        var input = InputDataService.Instance;
        var routing = AppRoutingModule.Instance;
        int published = 0;
        input.FileReplaced += OnFileReplaced;
        try
        {
            routing.NotifyInputMode("member");
            using var viewport = new ThreeService(new SceneService());
            using (var first = JsonDocument.Parse("""
                {"node":{"1":{"x":0},"2":{"x":5}},
                 "member":{"3":{"ni":"1","nj":"2","e":"1"}},
                 "load":{"1":{"load_node":[{"row":1,"n":"1","tx":2}]}}}
                """))
                input.JsonDataOpen(first.RootElement);
            viewport.FlushPending();
            Assert.Equal(2, viewport.NodeCount);
            Assert.Equal(1, viewport.MemberCount);
            Assert.Equal(1, published);

            using (var invalid = JsonDocument.Parse("""
                {"node":{"4":{"x":9}},
                 "member":{"6":{"ni":"4","nj":"4","e":"2"}},
                 "load":{"2":{"load_node":[{"row":1,"n":"4","tx":3}]}},
                 "result":{"Case1":{"disg":{},"fsec":{"6":{"0":{"fxi":"bad"}}}}}}
                """))
                Assert.Throws<JsonException>(() => input.JsonDataOpen(invalid.RootElement));

            viewport.FlushPending();
            Assert.Equal(1, published);
            Assert.Equal(2, viewport.NodeCount);
            Assert.Equal(1, viewport.MemberCount);
            Assert.Contains("3", InputMembersService.Instance.getMemberJson().Keys);
            Assert.DoesNotContain("6", InputMembersService.Instance.getMemberJson().Keys);
            Assert.Contains("1", InputLoadService.Instance.getLoadJson().Keys);
            Assert.DoesNotContain("2", InputLoadService.Instance.getLoadJson().Keys);
        }
        finally
        {
            input.FileReplaced -= OnFileReplaced;
            using var empty = JsonDocument.Parse("{}");
            input.JsonDataOpen(empty.RootElement);
            routing.NotifyInputMode("node");
        }

        void OnFileReplaced(long _) => published++;
    }

    [Fact]
    public void ThrowingLateObserverStillPublishesCompleteReplacement()
    {
        var input = InputDataService.Instance;
        var routing = AppRoutingModule.Instance;
        int published = 0;
        int laterObservers = 0;
        long beforeRevision = input.DocumentRevision;
        input.FileReplaced += OnFileReplaced;
        ResultReacService.Instance.Changed += ThrowingObserver;
        ResultReacService.Instance.Changed += LaterObserver;
        try
        {
            routing.NotifyInputMode("member");
            using var viewport = new ThreeService(new SceneService());
            using (var first = JsonDocument.Parse("""
                {"node":{"1":{"x":0},"2":{"x":5}},
                 "member":{"3":{"ni":"1","nj":"2","e":"1"}},
                 "load":{"1":{"load_node":[{"row":1,"n":"1","tx":2}]}}}
                """))
            {
                // The throwing subscriber is active only for the second document.
                ResultReacService.Instance.Changed -= ThrowingObserver;
                input.JsonDataOpen(first.RootElement);
                ResultReacService.Instance.Changed += ThrowingObserver;
            }
            viewport.FlushPending();

            using (var second = JsonDocument.Parse("""
                {"node":{"4":{"x":9},"5":{"x":12},"6":{"x":15}},
                 "member":{"6":{"ni":"4","nj":"5","e":"2"},
                           "7":{"ni":"5","nj":"6","e":"2"}},
                 "load":{"2":{"load_node":[{"row":1,"n":"4","tx":3}]}}}
                """))
            {
                var error = Assert.Throws<AggregateException>(() => input.JsonDataOpen(second.RootElement));
                Assert.Contains(error.InnerExceptions, e => e is InvalidOperationException);
            }

            viewport.FlushPending();
            Assert.Equal(beforeRevision + 2, input.DocumentRevision);
            Assert.Equal(2, published);
            Assert.True(laterObservers >= 2);
            Assert.Equal(3, viewport.NodeCount);
            Assert.Equal(2, viewport.MemberCount);
            Assert.Contains("6", InputMembersService.Instance.getMemberJson().Keys);
            Assert.DoesNotContain("3", InputMembersService.Instance.getMemberJson().Keys);
            Assert.Contains("2", InputLoadService.Instance.getLoadJson().Keys);
            Assert.DoesNotContain("1", InputLoadService.Instance.getLoadJson().Keys);
        }
        finally
        {
            ResultReacService.Instance.Changed -= ThrowingObserver;
            ResultReacService.Instance.Changed -= LaterObserver;
            input.FileReplaced -= OnFileReplaced;
            using var empty = JsonDocument.Parse("{}");
            input.JsonDataOpen(empty.RootElement);
            routing.NotifyInputMode("node");
        }

        void OnFileReplaced(long _) => published++;
        void ThrowingObserver(object? _, EventArgs __) => throw new InvalidOperationException("observer failure");
        void LaterObserver(object? _, EventArgs __) => laterObservers++;
    }

    [Fact]
    public void CameraFailurePreservesPreviousDocument()
    {
        var input = InputDataService.Instance;
        long beforeRevision;
        using (var first = JsonDocument.Parse("""
            {"node":{"1":{"x":0}},"member":{"3":{"ni":"1","nj":"1","e":"1"}}}
            """))
            input.JsonDataOpen(first.RootElement);
        beforeRevision = input.DocumentRevision;
        var camera = new FailingCameraScene();
        input.RegisterSceneService(camera);
        try
        {
            using var second = JsonDocument.Parse("""
                {"dimension":2,"node":{"4":{"x":9}},
                 "member":{"6":{"ni":"4","nj":"4","e":"2"}}}
                """);
            Assert.Throws<InvalidOperationException>(() => input.JsonDataOpen(second.RootElement));
            Assert.Equal(beforeRevision, input.DocumentRevision);
            Assert.Equal(3, input.dimension);
            Assert.Contains("3", InputMembersService.Instance.getMemberJson().Keys);
            Assert.DoesNotContain("6", InputMembersService.Instance.getMemberJson().Keys);
        }
        finally
        {
            input.UnregisterSceneService(camera);
            using var empty = JsonDocument.Parse("{}");
            input.JsonDataOpen(empty.RootElement);
        }
    }

    private sealed class FailingCameraScene : SceneService
    {
        internal override void ApplyDocumentCamera((float X, float Y, float Z)? position) =>
            throw new InvalidOperationException("camera failure");
    }
}
