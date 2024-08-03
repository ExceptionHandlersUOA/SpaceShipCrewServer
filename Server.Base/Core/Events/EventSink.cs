﻿using Server.Base.Core.Abstractions;
using Server.Base.Core.Events.Arguments;
using Server.Base.Worlds.EventArguments;

namespace Server.Base.Core.Events;

public class EventSink : IEventSink
{
    public delegate void CrashedEventHandler(CrashedEventArgs @event);

    public delegate void CreateDataEventHandler();

    public delegate void InternalShutdownEventHandler();

    public delegate void ServerStartedEventHandler(ServerStartedEventArgs @event);

    public delegate void ChangedOperationalModeEventHandler();

    public delegate void ShutdownEventHandler();

    public delegate void SocketConnectEventHandler(SocketConnectEventArgs @event);

    public delegate void WorldBroadcastEventHandler(WorldBroadcastEventArgs @event);

    public delegate void WorldLoadEventHandler();

    public delegate void WorldSaveEventHandler(WorldSaveEventArgs @event);

    public event CrashedEventHandler Crashed;

    public event ChangedOperationalModeEventHandler ChangedOperationalMode;

    public event ShutdownEventHandler Shutdown;
    public event InternalShutdownEventHandler InternalShutdown;

    public event ServerStartedEventHandler ServerStarted;
    public event SocketConnectEventHandler SocketConnect;

    public event CreateDataEventHandler CreateData;

    public event WorldLoadEventHandler WorldLoad;
    public event WorldBroadcastEventHandler WorldBroadcast;

    public void InvokeCrashed(CrashedEventArgs @event) => Crashed?.Invoke(@event);

    public void InvokeChangedOperationalMode() =>
        ChangedOperationalMode?.Invoke();

    public void InvokeShutdown() => Shutdown?.Invoke();
    public void InvokeInternalShutdown() => InternalShutdown?.Invoke();

    public void InvokeServerStarted(ServerStartedEventArgs e) => ServerStarted?.Invoke(e);
    public void InvokeSocketConnect(SocketConnectEventArgs e) => SocketConnect?.Invoke(e);

    public void InvokeCreateData() => CreateData?.Invoke();

    public void InvokeWorldLoad() => WorldLoad?.Invoke();
    public void InvokeWorldBroadcast(WorldBroadcastEventArgs e) => WorldBroadcast?.Invoke(e);
}
