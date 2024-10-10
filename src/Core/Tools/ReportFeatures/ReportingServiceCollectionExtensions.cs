﻿
using Core.Tools.ReportFeatures.OrganizationReportMembers.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Tools.ReportFeatures;

public static class ReportingServiceCollectionExtensions
{
    public static void AddReportingServices(this IServiceCollection services)
    {
        services.AddScoped<IMemberAccessCipherDetailsQuery, MemberAccessCipherDetailsQuery>();
    }
}
