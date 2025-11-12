using System.Collections;
using System.Collections.Generic;

namespace Dreambit;

public interface ICoroutineService
{
    CoroutineHandle StartCoroutine(IEnumerator routine);
    void StopCoroutine(CoroutineHandle handle);
    void StopAllCoroutines(object owner = null);
    bool IsRunning(CoroutineHandle handle);
}