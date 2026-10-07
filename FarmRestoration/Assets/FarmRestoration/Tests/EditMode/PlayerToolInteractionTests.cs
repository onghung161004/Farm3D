using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class PlayerToolInteractionTests
    {
        private GameObject controllerObject;
        private GameObject targetObject;

        [TearDown]
        public void TearDown()
        {
            if (controllerObject != null)
            {
                Object.DestroyImmediate(controllerObject);
            }

            if (targetObject != null)
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void TryUse_NoTarget_ReturnsFalseWithoutInvokingInteraction()
        {
            bool didUse = PlayerToolInteraction.TryUse(null, FarmTool.Hoe);

            Assert.That(didUse, Is.False);
        }

        [Test]
        public void TryUse_ValidInteractable_ForwardsSelectedToolExactlyOnce()
        {
            TestInteractable target = CreateTarget();

            bool didUse = PlayerToolInteraction.TryUse(target, FarmTool.WateringCan);

            Assert.That(didUse, Is.True);
            Assert.That(target.InteractionCount, Is.EqualTo(1));
            Assert.That(target.LastTool, Is.EqualTo(FarmTool.WateringCan));
        }

        [Test]
        public void TryGetInteractable_ComponentCollection_ResolvesInterfaceImplementerWithoutFarmPlotType()
        {
            targetObject = new GameObject("InteractionTargetTest");
            TestInteractable target = targetObject.AddComponent<TestInteractable>();
            List<MonoBehaviour> components = new List<MonoBehaviour>();
            targetObject.GetComponents(components);

            bool found = InteractionTargetResolver.TryGetInteractable(components, out IInteractable resolved);

            Assert.That(found, Is.True);
            Assert.That(resolved, Is.SameAs(target));
        }

        [Test]
        public void TryUseTool_OutOfRangeTargetQuery_ReturnsFalseWithoutInvokingInteractable()
        {
            controllerObject = new GameObject("PlayerControllerTest");
            PlayerToolController controller = controllerObject.AddComponent<PlayerToolController>();
            TestInteractable outOfRangeTarget = CreateTarget();
            outOfRangeTarget.transform.position = Vector3.forward * 4f;
            OutOfRangeTargetQuery query = new OutOfRangeTargetQuery(outOfRangeTarget);
            controller.ConfigureTargetQuery(query);

            bool didUse = controller.TryUseTool();

            Assert.That(didUse, Is.False);
            Assert.That(query.Candidate, Is.SameAs(outOfRangeTarget));
            Assert.That(query.QueryCount, Is.EqualTo(1));
            Assert.That(outOfRangeTarget.InteractionCount, Is.EqualTo(0));
        }

        [Test]
        public void TryUseTool_Success_PublishesToolAndTargetOnce()
        {
            controllerObject = new GameObject("PlayerControllerTest");
            PlayerToolController controller = controllerObject.AddComponent<PlayerToolController>();
            TestInteractable target = CreateTarget();
            controller.ConfigureTargetQuery(new OutOfRangeTargetQuery(target));
            int eventCount = 0;
            controller.InteractionSucceeded += (tool, interacted) =>
            {
                eventCount++;
                Assert.That(tool, Is.EqualTo(FarmTool.Hoe));
                Assert.That(interacted, Is.SameAs(target));
            };

            Assert.That(controller.TryUseTool(), Is.True);
            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public void TryUseTool_FailedAction_DoesNotPublishSuccess()
        {
            controllerObject = new GameObject("PlayerControllerTest");
            PlayerToolController controller = controllerObject.AddComponent<PlayerToolController>();
            TestInteractable target = CreateTarget();
            target.ShouldSucceed = false;
            controller.ConfigureTargetQuery(new OutOfRangeTargetQuery(target));
            int eventCount = 0;
            controller.InteractionSucceeded += (_, __) => eventCount++;

            Assert.That(controller.TryUseTool(), Is.False);
            Assert.That(target.InteractionCount, Is.EqualTo(1));
            Assert.That(eventCount, Is.Zero);
        }

        private TestInteractable CreateTarget()
        {
            targetObject = new GameObject("InteractionTargetTest");
            return targetObject.AddComponent<TestInteractable>();
        }
    }

    public sealed class OutOfRangeTargetQuery : IInteractionTargetQuery
    {
        private readonly IInteractable outOfRangeTarget;

        public OutOfRangeTargetQuery(IInteractable outOfRangeTarget)
        {
            this.outOfRangeTarget = outOfRangeTarget;
        }

        public int QueryCount { get; private set; }

        public IInteractable Candidate => outOfRangeTarget;

        public bool TryFindTarget(Vector3 origin, float range, out IInteractable target)
        {
            QueryCount++;
            MonoBehaviour candidateBehaviour = outOfRangeTarget as MonoBehaviour;
            if (candidateBehaviour != null
                && (candidateBehaviour.transform.position - origin).sqrMagnitude <= range * range)
            {
                target = outOfRangeTarget;
                return true;
            }

            target = null;
            return false;
        }
    }

    public sealed class TestInteractable : MonoBehaviour, IInteractable
    {
        public bool ShouldSucceed { get; set; } = true;
        public int InteractionCount { get; private set; }

        public FarmTool LastTool { get; private set; }

        public bool TryInteract(FarmTool tool)
        {
            InteractionCount++;
            LastTool = tool;
            return ShouldSucceed;
        }

        public string GetInteractionPrompt(FarmTool tool)
        {
            return "Interact";
        }
    }
}
