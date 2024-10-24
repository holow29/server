﻿using Bit.Api.AdminConsole.Models.Request;
using Bit.Api.Models.Response;
using Bit.Core;
using Bit.Core.AdminConsole.Enums;
using Bit.Core.AdminConsole.Models.Api.Response;
using Bit.Core.AdminConsole.OrganizationFeatures.Policies;
using Bit.Core.AdminConsole.OrganizationFeatures.Policies.Models;
using Bit.Core.AdminConsole.Repositories;
using Bit.Core.AdminConsole.Services;
using Bit.Core.Auth.Models.Business.Tokenables;
using Bit.Core.Context;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Tokens;
using Bit.Core.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.AdminConsole.Controllers;

[Route("organizations/{orgId}/policies")]
[Authorize("Application")]
public class PoliciesController : Controller
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IPolicyService _policyService;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IUserService _userService;
    private readonly ICurrentContext _currentContext;
    private readonly GlobalSettings _globalSettings;
    private readonly IDataProtector _organizationServiceDataProtector;
    private readonly IDataProtectorTokenFactory<OrgUserInviteTokenable> _orgUserInviteTokenDataFactory;
    private readonly IFeatureService _featureService;
    private readonly IReadOnlyDictionary<PolicyType, IPolicyValidator> _policyValidators;

    public PoliciesController(
        IPolicyRepository policyRepository,
        IPolicyService policyService,
        IOrganizationUserRepository organizationUserRepository,
        IUserService userService,
        ICurrentContext currentContext,
        GlobalSettings globalSettings,
        IDataProtectionProvider dataProtectionProvider,
        IDataProtectorTokenFactory<OrgUserInviteTokenable> orgUserInviteTokenDataFactory,
        IFeatureService featureService,
        IEnumerable<IPolicyValidator> validators)
    {
        _policyRepository = policyRepository;
        _policyService = policyService;
        _organizationUserRepository = organizationUserRepository;
        _userService = userService;
        _currentContext = currentContext;
        _globalSettings = globalSettings;
        _organizationServiceDataProtector = dataProtectionProvider.CreateProtector(
            "OrganizationServiceDataProtector");

        _orgUserInviteTokenDataFactory = orgUserInviteTokenDataFactory;
        _featureService = featureService;

        var dictionary = new Dictionary<PolicyType, IPolicyValidator>();
        foreach (var validator in validators)
        {
            dictionary.TryAdd(validator.Type, validator);
        }
        _policyValidators = dictionary;
    }

    [HttpGet("{type}")]
    public async Task<PolicyResponseModel> Get(string orgId, int type)
    {
        var orgIdGuid = new Guid(orgId);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }
        var policy = await _policyRepository.GetByOrganizationIdTypeAsync(orgIdGuid, (PolicyType)type);
        if (policy == null)
        {
            throw new NotFoundException();
        }

        if (_featureService.IsEnabled(FeatureFlagKeys.AccountDeprovisioning))
        {
            var canToggle = _policyValidators.ContainsKey(policy.Type) && string.IsNullOrWhiteSpace(
                await _policyValidators[policy.Type]
                    .ValidateAsync(
                        new PolicyUpdate
                        {
                            Data = policy.Data,
                            Enabled = !policy.Enabled,
                            OrganizationId = policy.OrganizationId,
                            Type = policy.Type
                        }, policy));

            return new PolicyResponseModel(policy, canToggle);
        }

        return new PolicyResponseModel(policy);
    }

    [HttpGet("")]
    public async Task<ListResponseModel<PolicyResponseModel>> Get(string orgId)
    {
        var orgIdGuid = new Guid(orgId);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }

        var policies = await _policyRepository.GetManyByOrganizationIdAsync(orgIdGuid);

        if (!_featureService.IsEnabled(FeatureFlagKeys.AccountDeprovisioning))
        {
            return new ListResponseModel<PolicyResponseModel>(policies.Select(p => new PolicyResponseModel(p)));
        }

        var responses = new List<PolicyResponseModel>();

        foreach (var policy in policies)
        {
            var canToggle = _policyValidators.ContainsKey(policy.Type) && string.IsNullOrWhiteSpace(
                await _policyValidators[policy.Type]
                    .ValidateAsync(
                        new PolicyUpdate
                        {
                            Data = policy.Data,
                            Enabled = !policy.Enabled,
                            OrganizationId = policy.OrganizationId,
                            Type = policy.Type
                        }, policy));

            responses.Add(new PolicyResponseModel(policy, canToggle));
        }

        return new ListResponseModel<PolicyResponseModel>(responses);
    }

    [AllowAnonymous]
    [HttpGet("token")]
    public async Task<ListResponseModel<PolicyResponseModel>> GetByToken(Guid orgId, [FromQuery] string email,
        [FromQuery] string token, [FromQuery] Guid organizationUserId)
    {
        // TODO: PM-4142 - remove old token validation logic once 3 releases of backwards compatibility are complete
        var newTokenValid = OrgUserInviteTokenable.ValidateOrgUserInviteStringToken(
            _orgUserInviteTokenDataFactory, token, organizationUserId, email);

        var tokenValid = newTokenValid || CoreHelpers.UserInviteTokenIsValid(
            _organizationServiceDataProtector, token, email, organizationUserId, _globalSettings
        );

        if (!tokenValid)
        {
            throw new NotFoundException();
        }

        var orgUser = await _organizationUserRepository.GetByIdAsync(organizationUserId);
        if (orgUser == null || orgUser.OrganizationId != orgId)
        {
            throw new NotFoundException();
        }

        var policies = await _policyRepository.GetManyByOrganizationIdAsync(orgId);
        var responses = policies.Where(p => p.Enabled).Select(p => new PolicyResponseModel(p));
        return new ListResponseModel<PolicyResponseModel>(responses);
    }

    // TODO: PM-4097 - remove GetByInvitedUser once all clients are updated to use the GetMasterPasswordPolicy endpoint below
    [Obsolete("Deprecated API", false)]
    [AllowAnonymous]
    [HttpGet("invited-user")]
    public async Task<ListResponseModel<PolicyResponseModel>> GetByInvitedUser(Guid orgId, [FromQuery] Guid userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new UnauthorizedAccessException();
        }
        var orgUsersByUserId = await _organizationUserRepository.GetManyByUserAsync(user.Id);
        var orgUser = orgUsersByUserId.SingleOrDefault(u => u.OrganizationId == orgId);
        if (orgUser == null)
        {
            throw new NotFoundException();
        }
        if (orgUser.Status != OrganizationUserStatusType.Invited)
        {
            throw new UnauthorizedAccessException();
        }

        var policies = await _policyRepository.GetManyByOrganizationIdAsync(orgId);
        var responses = policies.Where(p => p.Enabled).Select(p => new PolicyResponseModel(p));
        return new ListResponseModel<PolicyResponseModel>(responses);
    }

    [HttpGet("master-password")]
    public async Task<PolicyResponseModel> GetMasterPasswordPolicy(Guid orgId)
    {
        var userId = _userService.GetProperUserId(User).Value;

        var orgUser = await _organizationUserRepository.GetByOrganizationAsync(orgId, userId);

        if (orgUser == null)
        {
            throw new NotFoundException();
        }

        var policy = await _policyRepository.GetByOrganizationIdTypeAsync(orgId, PolicyType.MasterPassword);

        if (policy == null || !policy.Enabled)
        {
            throw new NotFoundException();
        }

        return new PolicyResponseModel(policy);
    }

    [HttpPut("{type}")]
    public async Task<PolicyResponseModel> Put(string orgId, int type, [FromBody] PolicyRequestModel model)
    {
        var orgIdGuid = new Guid(orgId);
        if (!await _currentContext.ManagePolicies(orgIdGuid))
        {
            throw new NotFoundException();
        }
        var policy = await _policyRepository.GetByOrganizationIdTypeAsync(new Guid(orgId), (PolicyType)type);
        if (policy == null)
        {
            policy = model.ToPolicy(orgIdGuid);
        }
        else
        {
            policy = model.ToPolicy(policy);
        }

        var userId = _userService.GetProperUserId(User);
        await _policyService.SaveAsync(policy, userId);
        return new PolicyResponseModel(policy);
    }
}
