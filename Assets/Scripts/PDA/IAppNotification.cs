using System;

// app content that can light up the notification dot on its home screen icon
public interface IAppNotification
{
    bool HasNotification { get; }

    event Action NotificationChanged;
}
