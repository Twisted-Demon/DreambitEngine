# Coroutines

Coroutines are `IEnumerator` methods scheduled by the current scene. Components
can use their protected `CoroutineService`; other code can use
`Scene.Instance.CoroutineService`.

```csharp
private IEnumerator FlashAfterDelay()
{
    yield return new WaitForSeconds(0.5f);
    _drawer.WithTint(Color.White);
    yield return new WaitForFrames(2);
    _drawer.WithTint(Color.Red);
}

public override void OnCreated()
{
    CoroutineService.StartCoroutine(FlashAfterDelay());
}
```

## Yield instructions

| Instruction | Resumes after |
| --- | --- |
| `WaitForSeconds(seconds)` | Scaled time; pass `true` for unscaled time |
| `WaitForFrames(count)` | The requested update frames |
| `WaitUntil(predicate)` | The predicate becomes true |
| `WaitWhile(predicate)` | The predicate becomes false |
| `WaitForTask(task)` | A .NET task completes |
| `WaitForFixedUpdate` | The next physics update |
| `WaitForEndOfFrame` | The end-of-frame scheduler phase |

Keep the returned `CoroutineHandle` when you may need to stop a routine:

```csharp
var handle = CoroutineService.StartCoroutine(Routine());
if (CoroutineService.IsRunning(handle))
    CoroutineService.StopCoroutine(handle);
```

Scene termination owns scheduler cleanup. Avoid routines that capture entities
from another scene.

