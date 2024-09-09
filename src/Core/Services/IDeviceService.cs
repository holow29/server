﻿using Bit.Core.Auth.Models.Api.Request;
using Bit.Core.Entities;

#nullable enable

namespace Bit.Core.Services;

public interface IDeviceService
{
    Task SaveAsync(Device device);
    Task ClearTokenAsync(Device device);
    Task DeleteAsync(Device device);
    Task UpdateDevicesTrustAsync(string currentDeviceIdentifier,
        Guid currentUserId,
        DeviceKeysUpdateRequestModel currentDeviceUpdate,
        IEnumerable<OtherDeviceKeysUpdateRequestModel> alteredDevices);
}
