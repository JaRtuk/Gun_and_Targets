using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CannonAcceptanceTests
{
    CannonController cannon;
    TargetSpawner spawner;
    Keyboard keyboard;
    readonly List<string> report = new List<string>();
    [UnitySetUp]
    public IEnumerator Setup()
    {
        yield return SceneManager.LoadSceneAsync("CannonRange");
        yield return null;
        cannon = Object.FindFirstObjectByType<CannonController>();
        spawner = Object.FindFirstObjectByType<TargetSpawner>();
        keyboard = InputSystem.AddDevice<Keyboard>();
        Assert.IsNotNull(cannon); Assert.IsNotNull(spawner);
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        Time.timeScale = 1;
        yield return null;
    }
    IEnumerator Hold(Key key)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
        yield return new WaitForSeconds(0.2f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return null;
    }
    void CheckLine()
    {
        var line = cannon.trajectory.GetComponent<LineRenderer>();
        Assert.Less(Vector3.Distance(line.GetPosition(0),cannon.muzzle.position),0.001f);
        Assert.Greater(line.positionCount,20);
    }
    [UnityTest]
    public IEnumerator A_B_KeyboardMovementRotationFireAndTrajectory()
    {
        spawner.gameObject.SetActive(false);
        foreach (var t in Object.FindObjectsByType<Target>(FindObjectsSortMode.None)) Object.Destroy(t.gameObject);
        CheckLine();
        foreach (Key key in new[] {Key.W,Key.A,Key.S,Key.D})
        {
            Vector3 before = cannon.transform.position;
            yield return Hold(key);
            Vector3 delta = cannon.transform.position-before;
            Vector3 expected = key == Key.W ? Vector3.forward : key == Key.S ? Vector3.back : key == Key.A ? Vector3.left : Vector3.right;
            Assert.Greater(Vector3.Dot(delta,expected),0.2f,key.ToString()); CheckLine();
        }
        foreach (Key key in new[] {Key.Q,Key.E})
        {
            Vector3 forward = cannon.muzzle.forward; Vector3 endpoint = cannon.trajectory.Endpoint;
            yield return Hold(key);
            Assert.Greater(Vector3.Angle(forward,cannon.muzzle.forward),1);
            Assert.Greater(Vector3.Distance(endpoint,cannon.trajectory.Endpoint),0.5f); CheckLine();
        }
        int shots = cannon.Shots;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));
        yield return null; yield return null;
        Assert.AreEqual(shots+1,cannon.Shots);
        Assert.IsNotNull(cannon.LastProjectile);
        Assert.Less(Vector3.Angle(cannon.muzzle.forward,cannon.LastProjectile.Body.linearVelocity),3);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
    }
    [UnityTest]
    public IEnumerator C_D_FiveRealFlightsMatchSavedPreview()
    {
        spawner.gameObject.SetActive(false);
        foreach (var t in Object.FindObjectsByType<Target>(FindObjectsSortMode.None)) Object.Destroy(t.gameObject);
        yield return null;
        Random.InitState(20261021);
        report.Clear();
        report.Add("# Фактические полёты Unity Rigidbody / PlayMode\n\nШаг 0.02 s; скорость 35 m/s; без мишеней; реальная позиция — Rigidbody.position в первом OnCollisionEnter, прогноз — центр сферы при SphereCast.\n");
        report.Add("| № | Масса до / ядро (kg) | Радиус до / ядро (m) | Следующие m / r | Скорость | Прогноз центра | Реальный центр | Ошибка (m) |\n|---|---|---|---|---|---|---|---|");
        var pairs = new HashSet<Vector2>(); float maxError = 0;
        for (int i=0;i<5;i++)
        {
            float m=cannon.NextMass,r=cannon.NextRadius;
            cannon.RefreshPrediction(); Assert.IsTrue(cannon.trajectory.HasContact);
            Vector3 prediction=cannon.trajectory.Endpoint;
            var ball=cannon.Fire();
            Assert.AreEqual(m,ball.Mass); Assert.AreEqual(r,ball.Radius);
            Assert.AreEqual(m,ball.Body.mass); Assert.AreEqual(0,ball.Body.linearDamping);
            Assert.AreEqual(cannon.NextMass,cannon.trajectory.Mass); Assert.AreEqual(cannon.NextRadius,cannon.trajectory.Radius);
            Assert.That(m,Is.InRange(2f,6f)); Assert.That(r,Is.InRange(0.1f,0.2f));
            Assert.Less(Mathf.Abs(ball.GetComponent<SphereCollider>().radius*ball.transform.lossyScale.x-r),0.00001f);
            Assert.Less(Vector3.Distance(ball.Body.linearVelocity,cannon.muzzle.forward*35),0.001f);
            pairs.Add(new Vector2(m,r));
            float deadline=Time.time+9;
            while (ball && !ball.HasContact && Time.time<deadline) yield return new WaitForFixedUpdate();
            Assert.IsTrue(ball && ball.HasContact,"No ground contact before timeout");
            float error=Vector3.Distance(prediction,ball.FirstContactCenter); maxError=Mathf.Max(maxError,error);
            report.Add($"| {i+1} | {m:F5} / {ball.Mass:F5} | {r:F5} / {ball.Radius:F5} | {cannon.NextMass:F5} / {cannon.NextRadius:F5} | 35 | {prediction.ToString("F4")} | {ball.FirstContactCenter.ToString("F4")} | {error:F5} |");
            Directory.CreateDirectory("Reports"); File.WriteAllLines("Reports/PhysicsFlights.md",report);
            Assert.LessOrEqual(error,1,$"Shot {i+1}: {error} m");
            Object.Destroy(ball.gameObject); yield return null;
        }
        Assert.AreEqual(5,pairs.Count);
        report.Add($"\nМаксимальная ошибка: **{maxError:F5} m**. Все 5 выстрелов в допуске 1 m. Параметры предпросмотра и созданного ядра совпали во всех случаях.");
        File.WriteAllLines("Reports/PhysicsFlights.md",report);
    }
    [UnityTest]
    public IEnumerator E_AutomaticTargetsHaveCorrectPhysicsCapMotionAndLifetime()
    {
        foreach (var t in Object.FindObjectsByType<Target>(FindObjectsSortMode.None)) Object.Destroy(t.gameObject);
        yield return null;
        spawner.interval=0.1f; spawner.lifetime=1.5f;
        yield return new WaitForSeconds(3.2f);
        Assert.AreEqual(4,spawner.Living.Count);
        var originals=spawner.Living.ToArray(); var target=originals[0]; Vector3 start=target.transform.position;
        foreach(var t in originals)
        {
            var rb=t.GetComponent<Rigidbody>(); var collider=t.GetComponent<SphereCollider>();
            Assert.That(rb.mass,Is.InRange(1f,5f)); Assert.That(t.Radius,Is.InRange(0.4f,1f));
            Assert.IsFalse(rb.useGravity); Assert.That(rb.linearVelocity.magnitude,Is.InRange(2f,5f));
            Assert.Less(Mathf.Abs(rb.linearVelocity.y),0.001f);
            Assert.Less(Mathf.Abs(collider.radius*t.transform.lossyScale.x-t.Radius),0.00001f);
            Assert.That(t.transform.position.y,Is.EqualTo(8).Within(0.01));
        }
        yield return new WaitForSeconds(0.15f);
        Assert.Greater(Vector3.Distance(start,target.transform.position),0.1f);
        spawner.enabled=false;
        yield return new WaitForSeconds(1.6f);
        foreach(var t in originals) Assert.IsTrue(t==null,"Lifetime failed");
    }
    [UnityTest]
    public IEnumerator F_OnlyProjectileHitsCountOncePerTarget()
    {
        spawner.enabled=false;
        foreach(var t in Object.FindObjectsByType<Target>(FindObjectsSortMode.None)) Object.Destroy(t.gameObject);
        yield return null;
        var target=Object.Instantiate(spawner.targetPrefab,new Vector3(0,5,10),Quaternion.identity);
        target.GetComponent<Rigidbody>().isKinematic=true;
        int hits=cannon.Hits;
        var other=GameObject.CreatePrimitive(PrimitiveType.Sphere); other.transform.position=new Vector3(0,5,8);
        var body=other.AddComponent<Rigidbody>(); body.useGravity=false; body.linearVelocity=Vector3.forward*10;
        yield return new WaitForSeconds(0.4f); Assert.AreEqual(hits,cannon.Hits); Object.Destroy(other);
        for(int i=0;i<2;i++)
        {
            var ball=Object.Instantiate(cannon.projectilePrefab,new Vector3(0,5,8),Quaternion.identity);
            ball.Owner=cannon; ball.SetPhysicalParams(4,0.15f,0.47f,1.225f,Vector3.zero,Vector3.forward*10);
            yield return new WaitForSeconds(0.35f); Assert.AreEqual(hits+1,cannon.Hits); Object.Destroy(ball.gameObject);
        }
        Object.Destroy(target.gameObject);
    }
}
