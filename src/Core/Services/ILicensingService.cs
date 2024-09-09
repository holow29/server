﻿using Bit.Core.AdminConsole.Entities;
using Bit.Core.Entities;
using Bit.Core.Models.Business;

#nullable enable

namespace Bit.Core.Services;

public interface ILicensingService
{
    Task ValidateOrganizationsAsync();
    Task ValidateUsersAsync();
    Task<bool> ValidateUserPremiumAsync(User user);
    bool VerifyLicense(ILicense license);
    byte[] SignLicense(ILicense license);
    Task<OrganizationLicense?> ReadOrganizationLicenseAsync(Organization organization);
    Task<OrganizationLicense?> ReadOrganizationLicenseAsync(Guid organizationId);

}
