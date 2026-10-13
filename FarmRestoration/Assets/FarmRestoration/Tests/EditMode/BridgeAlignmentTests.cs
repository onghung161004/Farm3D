using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class BridgeAlignmentTests
    {
        private GameObject root, model;
        private MeshCollider deck;
        private BridgeWidening widening;
        private Mesh source;
        [SetUp]
        public void Setup()
        {
            root = new GameObject("AlignmentFixture");
            model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RestorationSourceBridge.fbx")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Polytope Studio/Lowpoly_Village/Sources/Meshes/Modular/Bridge/PT_Wooden_Bridge_02.fbx"), root.transform);
            model.transform.SetPositionAndRotation(new Vector3(-46.148766f,3.497434f,41.89164f), new Quaternion(-.0247814f,.24258925f,-.09855708f,.9647916f));
            model.transform.localScale = new Vector3(4.5639205f,1,1);
            // Match FarmDemo: full imported-body colliders are disabled in favour of the dedicated walkway.
            foreach(Collider collider in model.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            var go = new GameObject("ActualDeck",typeof(MeshCollider)); go.transform.SetParent(root.transform);
            deck=go.GetComponent<MeshCollider>();
            source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/BridgeDiagnosticWalkway.asset")
                ?? AssetDatabase.LoadAssetAtPath<Mesh>("Assets/FarmRestoration/Models/Generated/PolytopeBridgeWalkway.asset");
            Assert.That(source,Is.Not.Null); deck.sharedMesh=source;
            var filters=model.GetComponentsInChildren<MeshFilter>();
            widening=BridgeWidening.Install(model.transform,deck,filters,System.Array.ConvertAll(filters,f=>f.sharedMesh));
        }
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(root);
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ActualSceneColliderCannotSupportOutsideVisibleBridge(int level)
        {
            Assert.That(widening.Apply(level),Is.True);
            Vector3 outside=model.transform.TransformPoint(new Vector3(1.29365f,0,-.11248f+1.2f*(level+1)));
            Assert.That(deck.Raycast(new Ray(outside+Vector3.up*20,Vector3.down),out _,40),Is.False,
                "Old3.3m support must not expand independently of the1.79m model.");
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void WalkingSurfaceHeightMatchesActualPlankTop(int level)
        {
            widening.Apply(level);
            MeshFilter filter=System.Array.Find(model.GetComponentsInChildren<MeshFilter>(),f=>f.name.EndsWith("LOD0"));
            var probe=new GameObject("PlankProbe",typeof(MeshCollider));probe.transform.SetParent(root.transform);
            probe.transform.SetPositionAndRotation(filter.transform.position,filter.transform.rotation);probe.transform.localScale=filter.transform.lossyScale;
            var real=probe.GetComponent<MeshCollider>();real.sharedMesh=filter.sharedMesh;Physics.SyncTransforms();
            foreach(float x in new[]{.13471f,1.29365f,2.46168f})
            {
                Vector3 point=model.transform.TransformPoint(new Vector3(x,0,-.11248f));
                Ray ray=new Ray(point+model.transform.up*20,-model.transform.up);
                Assert.That(real.Raycast(ray,out RaycastHit visible,40),Is.True);
                Assert.That(deck.Raycast(ray,out RaycastHit supported,40),Is.True);
                Assert.That(supported.point.y-visible.point.y,Is.InRange(-.015f,.04f));
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void RestoredGuardsStayNearVisibleRailInsteadOfOldWideCollider(int level)
        {
            var railing=BridgeRailingCollision.Ensure(deck); widening.Apply(level);railing.SetRestored(true);
            Vector3 point=model.transform.TransformPoint(new Vector3(1.29365f,0,-.11248f));
            Ray ray=new Ray(point+Vector3.up*20,Vector3.down);
            Assert.That(deck.Raycast(ray,out RaycastHit floor,40),Is.True);
            Physics.SyncTransforms();
            Vector3 side=Vector3.ProjectOnPlane(model.transform.forward,Vector3.up).normalized;
            Assert.That(Physics.SphereCast(floor.point+Vector3.up*.65f,.2f,side,out RaycastHit hit,8,
                Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),Is.True);
            Assert.That(hit.collider,Is.InstanceOf<BoxCollider>());
            Assert.That(hit.distance,Is.LessThan(.9f*(level+1)),"No barrier at the old1.65m half-width.");
        }
    }
}
