using System;
using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public abstract class DisposableObject : IDisposable
{
    private bool _isDisposed;

    ~DisposableObject()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void CleanUp()
    {
        
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if(disposing)
            CleanUp();

        _isDisposed = true;
    }
}