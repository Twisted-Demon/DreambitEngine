using System;

namespace PixelariaEngine;

public abstract class DisposableObject : IDisposable
{
    private bool _isDisposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DisposableObject()
    {
        Dispose(false);
    }

    protected virtual void CleanUp()
    {
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
            CleanUp();

        _isDisposed = true;
    }
}