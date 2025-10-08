using Plate.CrossMilo.Contracts.Game;
using Plate.CrossMilo.Contracts.Input;

// Type aliases for IService pattern
using IInputRouter = Plate.CrossMilo.Contracts.Input.Router.IService;
using IInputScope = Plate.CrossMilo.Contracts.Input.Scope.IService;

namespace WingedBean.Plugins.ConsoleDungeon.Input;

/// <summary>
/// Default stack-based input router.
/// </summary>
public class DefaultInputRouter : IInputRouter
{
    private readonly Stack<IInputScope> _scopes = new();
    private readonly object _lock = new();

    public IInputScope? Top
    {
        get
        {
            lock (_lock)
            {
                return _scopes.Count > 0 ? _scopes.Peek() : null;
            }
        }
    }

    public IDisposable PushScope(IInputScope scope)
    {
        lock (_lock)
        {
            _scopes.Push(scope);
        }
        return new ScopeHandle(this, scope);
    }

    public void Dispatch(GameInputEvent inputEvent)
    {
        IInputScope? currentScope;
        lock (_lock)
        {
            currentScope = Top;
        }

        currentScope?.Handle(inputEvent);
    }

    private void PopScope(IInputScope scope)
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
        private readonly IInputScope _scope;
        private bool _disposed = false;

        public ScopeHandle(DefaultInputRouter router, IInputScope scope)
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
