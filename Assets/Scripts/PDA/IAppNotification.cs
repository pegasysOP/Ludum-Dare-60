using System;

// app content that can light up the notification dot on its home screen icon
public interface IAppNotification
{
    bool HasNotification { get; }

    // keeps the phone buzzing until it's seen, otherwise it buzzes once when it lights up
    bool KeepsBuzzing { get; }

    event Action NotificationChanged;
}
