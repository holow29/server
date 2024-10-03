﻿using Bit.Core.Billing.Caches;
using Bit.Core.Billing.Caches.Implementations;
using Bit.Core.Billing.Licenses;
using Bit.Core.Billing.Services;
using Bit.Core.Billing.Services.Implementations;

namespace Bit.Core.Billing.Extensions;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddBillingOperations(this IServiceCollection services)
    {
        services.AddTransient<IOrganizationBillingService, OrganizationBillingService>();
        services.AddTransient<IPremiumUserBillingService, PremiumUserBillingService>();
        services.AddTransient<ISetupIntentCache, SetupIntentDistributedCache>();
        services.AddTransient<ISubscriberService, SubscriberService>();
        services.AddTransient<IValidateLicenseCommandHandler, ValidateLicenseCommandHandler>();
        services.AddTransient<IValidateEntityAgainstLicenseCommandHandler, ValidateEntityAgainstLicenseCommandHandler>();
    }
}
