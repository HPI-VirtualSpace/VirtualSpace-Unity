using System;
using EasyButtons;
using UnityEngine;
using VirtualSpace.Shared;
using Logger = VirtualSpace.Shared.Logger;

public class VSManager : MonoBehaviour
{
    public string ServerIp;
    public int ServerPort;

    private NetworkingClient _client;
    public NetworkEventHandler EventHandler = new NetworkEventHandler();

    public Users Users;

    [SerializeField]
    private long _virtualSpaceTurn;
    [SerializeField]
    private float _virtualSpaceSeconds;

    public float DeleteOlderThan = 10f;
    [Button]
    public void CleanEventsOlderThan()
    {
        var turn = VirtualSpaceTime.CurrentTurn - VirtualSpaceTime.ConvertSecondsToTurns(DeleteOlderThan);
        EventMap.Instance.CleanupSendBefore(turn);
    }

    void Awake()
    {
        Application.runInBackground = true;
        _client = new NetworkingClient(ServerIp, ServerPort);
    }

    void OnEnable()
    {
        _client.OnConnectionEstablished += HandleOnConnectionEstablished;
        _client.OnHandleMessage += HandleMessage;

        EventHandler.SetDefaultHandler(DefaultHandler);
        EventHandler.Attach(typeof(Incentives), HandleEventMessage);
        EventHandler.Attach(typeof(TimeMessage), HandleTimeMessage);
        EventHandler.Attach(typeof(PlayerPosition), HandleUserPosition);
        EventHandler.Attach(typeof(PreferencesMessage), HandlePreferenceMessage);
        EventHandler.Attach(typeof(FrontendPayload), HandleFrontendPayload);
        EventHandler.Attach(typeof(FrontendLogMessage), HandleLogMessage);

        _client.StartListening();
    }
    
    void OnDisable()
    {
        _client.OnConnectionEstablished = null;
        _client.OnHandleMessage = null;

        EventHandler.SetDefaultHandler(null);
        EventHandler.Detach(typeof(Incentives), HandleEventMessage);
        EventHandler.Detach(typeof(TimeMessage), HandleTimeMessage);
        EventHandler.Detach(typeof(PlayerPosition), HandleUserPosition);
        EventHandler.Detach(typeof(PreferencesMessage), HandlePreferenceMessage);
        EventHandler.Detach(typeof(FrontendPayload), HandleFrontendPayload);
        EventHandler.Detach(typeof(FrontendLogMessage), HandleLogMessage);

        _client.StopListening();
    }

    public void SendReliable(MessageBase messageBase)
    {
        messageBase.UserId = 100;
        if (_client != null)
            _client.SendReliable(messageBase);
    }

    void HandleLogMessage(IMessageBase messageBase)
    {
        FrontendLogMessage logMessage = (FrontendLogMessage) messageBase;
        var unityMessage = "Backend: " + logMessage.Message;
        switch (logMessage.LogLevel)
        {
            case Logger.Level.Trace:
            case Logger.Level.Debug:
            case Logger.Level.Info:
                Debug.Log(unityMessage);
                break;
            case Logger.Level.Warn:
                Debug.LogWarning(unityMessage);
                break;
            case Logger.Level.Error:
                Debug.LogError(unityMessage);
                break;
            case Logger.Level.Off:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void HandleOnConnectionEstablished()
    {
        Debug.Log("Connection was established");
        // register as Frontend
        _client.SendReliable(new FrontendRegistration());
    }

    void HandleMessage(IMessageBase messageBase)
    {
        EventHandler.Invoke(messageBase.GetType(), messageBase);
    }

    // Message handler
    void HandleEventMessage(IMessageBase messageBase)
    {
        Incentives incentives = (Incentives) messageBase;
        EventMap.Instance.AddOrModifyEvents(incentives.Events);
    }

    private TimeMessage _lastTimeMessage;
    void HandleTimeMessage(IMessageBase messageBase)
    {
        TimeMessage timeMessage = (TimeMessage) messageBase;
        _lastTimeMessage = timeMessage;
    }

    private void HandleFrontendPayload(IMessageBase messageBase)
    {
        FrontendPayload payloadMessage = (FrontendPayload) messageBase;
        HandleMessage(payloadMessage.Payload);
    }

    private void HandleUserPosition(IMessageBase messageBase)
    {
        PlayerPosition position = (PlayerPosition) messageBase;
        Users.SetUserPosition(position.UserId, position.Position.ToVector2());
    }

    void HandlePreferenceMessage(IMessageBase messageBase)
    {
        PreferencesMessage preferencesMessage = (PreferencesMessage) messageBase;

        var userId = preferencesMessage.UserId;
        var userPreferences = preferencesMessage.preferences;

        Users.SetUserColor(userId, userPreferences.Color);
    }

    void DefaultHandler(IMessageBase messageBase)
    {
        Debug.LogWarning("No handler for message " + messageBase.GetType());
    }

    void Update()
    {
        if (!_client.IsConnected())
        {
            _client.StartListening();
            return;
        }

        VirtualSpaceTime.SetUnityTime(Time.unscaledTime);

        if (_lastTimeMessage != null)
        {
            VirtualSpaceTime.Update(_lastTimeMessage.Millis, _lastTimeMessage.TripTime);
            _lastTimeMessage = null;
        }

        _virtualSpaceTurn = VirtualSpaceTime.CurrentTurn;
        _virtualSpaceSeconds = VirtualSpaceTime.ConvertTurnsToSeconds(_virtualSpaceTurn);
    }
}
