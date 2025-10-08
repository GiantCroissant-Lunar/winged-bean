using Plate.CrossMilo.Contracts.Game;
using Plate.CrossMilo.Contracts.Input;

namespace WingedBean.Providers.Input;

/// <summary>
/// Default stack-based input router.
/// </summary>
public class DefaultInputRouter : IService
{
    private readonly Stack<IService> _scopes = new();
    private readonly object _lock = new();

    public IService? Top
    {
        get
        {
            lock (_lock)
            {
                return _scopes.Count > 0 ? _scopes.Peek() : null;
            }
        }
    }

    public IDisposable PushScope(IService scope)
    {
        lock (_lock)
        {
            _scopes.Push(scope);
        }
        return new ScopeHandle(this, scope);
    }

    public void Dispatch(GameInputEvent inputEvent)
    {
        IService? currentScope;
        lock (_lock)
        {
            currentScope = Top;
        }

        currentScope?.Handle(inputEvent);
    }

    private void PopScope(IService scope)
    {
        lock (_lock)
        {
            if (_scopes.Count > 0 && _scopes.Peek() == scope)
            {
                _scopes.Pop();
            }
        }
    }

    private class ScopeHandle : IDisposable
    {
        private readonly DefaultInputRouter _router;
        private readonly IService _scope;
        private bool _disposed = false;

        public ScopeHandle(DefaultInputRouter router, IService scope)
        {
            _router = router;
            _scope = scope;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _router.PopScope(_scope);
                _disposed = true;
            }
        }
    }
}
