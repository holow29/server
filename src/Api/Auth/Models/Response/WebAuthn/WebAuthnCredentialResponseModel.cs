﻿using Bit.Core.Auth.Entities;
using Bit.Core.Models.Api;

namespace Bit.Api.Auth.Models.Response.WebAuthn;

public class WebAuthnCredentialResponseModel : ResponseModel
{
    private const string ResponseObj = "webauthnCredential";

    public WebAuthnCredentialResponseModel(WebAuthnCredential credential) : base(ResponseObj)
    {
        Id = credential.Id.ToString();
        Name = credential.Name;
        SupportsPrf = credential.SupportsPrf;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public bool SupportsPrf { get; set; }
}
