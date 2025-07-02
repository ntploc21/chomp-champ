using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader", order = 1)]
public class InputReader : ScriptableObject, GameInput.IPlayerActions
{
    #region Internal Data
    public event UnityAction<Vector2> OnMoveEvent;
    public event UnityAction OnSprintStartedEvent;
    public event UnityAction OnSprintStoppedEvent;
    public event UnityAction OnDashEvent;

    private GameInput _gameInput;
    #endregion

    private void OnEnable()
    {
        if (_gameInput == null)
        {
            _gameInput = new GameInput();
            _gameInput.Player.SetCallbacks(this);
        }

        _gameInput.Enable();
    }

    private void OnDisable()
    {
        if (_gameInput != null)
        {
            _gameInput.Disable();
        }
    }

    #region Input Callbacks
    public void OnMove(InputAction.CallbackContext context)
    {
        OnMoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnSprintStartedEvent?.Invoke();
        }
        else if (context.canceled)
        {
            OnSprintStoppedEvent?.Invoke();
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnDashEvent?.Invoke();
        }
    }
    #endregion
}
