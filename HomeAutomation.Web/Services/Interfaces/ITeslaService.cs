﻿namespace HomeAutomation.Web.Services.Interfaces;

public interface ITeslaService
{
    Task ToggleChargePort();
    Task OpenTrunk(bool front);
    Task UnlockChargePort();
    Task SetClimate(bool enable);
    Task SetTemperature(float temperature);
}
