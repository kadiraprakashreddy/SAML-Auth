# Migrating ASP.NET MVC 5 from Windows Authentication to SAML 2.0

This document describes how the SMAL Auth sample was moved from **IIS Windows Authentication** to **SAML 2.0 with Microsoft Entra ID**.

The original Windows-auth app is still in the solution as `SmalAuth`. The SAML implementation is `SmalAuth.Saml`. That second project is the migration target: same MVC 5 stack, different authentication.

RSA / hardware-token validation from the WebPlus diagrams is **not** in this spike. Entra MFA is handled by Entra Conditional Access, not by the application.

---

## 1. Starting point: Windows Authentication

The original MVC 5 app identified the user from IIS:

- `Web.config` used `<authentication mode="Windows" />`
- Anonymous access was denied (`<deny users="?" />`)
- IIS Express had **Windows Authentication enabled** and **Anonymous Authentication disabled**
- The browser sent Negotiate/Kerberos (or NTLM on localhost)
- Controllers read `User.Identity` as a `WindowsIdentity`

There was no login form. The user was whoever opened the browser.

That does not match the WebPlus design. WebPlus needs:

- Entra ID as the identity provider
- MFA when Entra requires it
- A SAML assertion posted back to an ACS endpoint
- The original URL restored through **RelayState**

So the app was migrated from IIS Windows Auth to a SAML service provider.

---

## 2. Target flow

```
/Customer/Edit/12345
        ↓
Not authenticated
        ↓
Create ReturnUrl / RelayState
        ↓
SAML AuthnRequest
        ↓
login.microsoftonline.com
        ↓
Entra login + MFA (if Conditional Access requires it)
        ↓
SAML Response / Assertion
        ↓
/Saml2/Acs
        ↓
Validate assertion
        ↓
Create application session
        ↓
/Customer/Edit/12345
```

This is **SAML 2.0**, not OIDC (`/signin-oidc`).

---

## 3. What we changed

| Area | Before (Windows Auth) | After (SAML) |
| --- | --- | --- |
| Authenticator | IIS (Negotiate / Kerberos / NTLM) | Entra ID |
| `Web.config` authentication | `mode="Windows"` | `mode="None"` |
| IIS anonymous | Disabled | Enabled (browser must reach SignIn and ACS) |
| IIS Windows Auth | Enabled | Disabled |
| Challenge | Browser 401 + Negotiate | Redirect to `/Saml2/SignIn` |
| Session | Windows token on each request | Session cookie after ACS |
| User id | `DOMAIN\username` | NameID + Azure object identifier |
| Deep links | Same Windows user, no redirect | Original URL stored as RelayState |
| MFA | Not part of IIS Windows Auth | Entra Conditional Access |

MVC 5, controllers, views, and routing stay the same. Only authentication changes.

---

## 4. Implementation steps

### Step 1. Keep the Windows-auth app, add a SAML app

Do not convert the working Windows-auth project in place until SAML is proven.

In this repo:

- `SmalAuth` — original IIS Windows Auth POC
- `SmalAuth.Saml` — SAML 2.0 + Entra POC

Both sit in `SmalAuth.sln`.

### Step 2. Add the SAML service-provider library

On the SAML project, install **Sustainsys.Saml2.Mvc** (2.11.0). That package provides:

- `/Saml2/SignIn` — starts the AuthnRequest
- `/Saml2/Acs` — Assertion Consumer Service
- `/Saml2/Logout` — SAML logout
- `/Saml2` — SP metadata

Also reference:

- `System.IdentityModel`
- `System.IdentityModel.Services`

### Step 3. Turn off Windows Authentication

In `SmalAuth.Saml\Web.config`:

```xml
<authentication mode="None" />
<authorization>
  <allow users="*" />
</authorization>
```

In IIS / IIS Express for the SAML site:

- Enable **Anonymous Authentication**
- Disable **Windows Authentication**

If Windows Auth stays on, IIS challenges the browser before SAML can run.

The Windows-auth site (`SmalAuth` on port 5000) still uses Windows Auth through an IIS Express location override. The SAML site does not.

### Step 4. Register SAML routes

`Saml2/{action}` must be registered **before** the default MVC route so Sustainsys can serve SignIn and ACS:

```csharp
routes.MapRoute(
    name: "Saml2",
    url: "Saml2/{action}",
    defaults: new { controller = "Saml2", action = "Index" }
);

routes.MapRoute(
    name: "Default",
    url: "{controller}/{action}/{id}",
    defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
);
```

### Step 5. Make SAML authentication global

Do **not** put `[SamlAuthorize]` on every controller. For a 400-page app such as WebPlus, register it once as a global MVC filter. Pages are protected by default. Only mark genuine public endpoints with `[AllowAnonymous]`.

In `FilterConfig.cs`:

```csharp
public static void RegisterGlobalFilters(GlobalFilterCollection filters)
{
    filters.Add(new HandleErrorAttribute());
    filters.Add(new SamlAuthorizeAttribute());
}
```

`Global.asax` already calls `FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters)` during `Application_Start`.

The attribute then:

1. Skips `[AllowAnonymous]` actions and controllers
2. Skips `/Saml2/SignIn`, `/Saml2/Acs`, `/Saml2/Logout`, and SP metadata so there is no redirect loop
3. If there is no session, takes the current URL (`/Customer/Edit/12345`)
4. Redirects to `/Saml2/SignIn?ReturnUrl=/Customer/Edit/12345`
5. Sustainsys puts that ReturnUrl into SAML **RelayState**
6. After ACS validates the assertion, the user is sent back to that URL

`/Home/Setup` is `[AllowAnonymous]` so missing Entra config can still be shown. `Home/Index` and `Customer/Edit` have no attribute — the global filter protects them.

Before enabling this on WebPlus, list the few endpoints that must stay public (health checks, error pages, integration callbacks) and mark only those `[AllowAnonymous]`.

### Step 6. Create the application session after ACS

Sustainsys validates the assertion at `/Saml2/Acs`, then `SessionAuthenticationModule` writes a session cookie (`SmalAuth.Saml`). Later requests use that cookie. They do not talk to Entra again until the session expires or the user signs out.

Required `Web.config` pieces:

- `SessionAuthenticationModule`
- `system.identityModel` audience = SP Entity ID
- `system.identityModel.services` cookie handler (`requireSsl="true"`)

### Step 7. Read the Entra identity, not the Windows identity

After SAML, `User.Identity` is a `ClaimsIdentity`, not a `WindowsIdentity`.

The user page reads:

- **NameID** — `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`
- **Azure ID** — `http://schemas.microsoft.com/identity/claims/objectidentifier` (fallback: `oid`)
- Email / name claims if Entra sends them
- The full claim set from the assertion

If Azure ID is empty, the Entra Enterprise Application is not emitting `objectidentifier`. Confirm the unique user identifier on the Entra app.

### Step 8. Configure Entra and the application together

Values used by this POC:

| Setting | Value |
| --- | --- |
| Tenant ID | `32761e73-fd53-4cfd-9dfe-946febf1aae4` |
| SP Entity ID | `https://localhost:44322/Saml2` |
| ACS / Reply URL | `https://localhost:44322/Saml2/Acs` |
| IdP Entity ID | `https://sts.windows.net/32761e73-fd53-4cfd-9dfe-946febf1aae4/` |
| Login / Logout | `https://login.microsoftonline.com/{tenant}/saml2` |
| Federation metadata | `https://login.microsoftonline.com/{tenant}/federationmetadata/2007-06/federationmetadata.xml?appid=82009cdd-38d2-4416-8d76-1ff399842494` |

On the **Entra Enterprise Application** (SAML):

1. Identifier (Entity ID) = `https://localhost:44322/Saml2`
2. Reply URL (ACS) = `https://localhost:44322/Saml2/Acs`
3. Sign-on URL can be `https://localhost:44322/`
4. Keep Unique User Identifier as Azure object ID / NameID as agreed with the office app registration
5. Assign users or groups who may sign in
6. Leave MFA to Conditional Access (“MFA, if applicable”)

In `Web.config`, set **both**:

- `appSettings` (`ida:TenantId`, `saml:*`)
- `<sustainsys.saml2>` `identityProviders` `entityId` and `metadataLocation`

Sustainsys reads the `<sustainsys.saml2>` section. Updating only `appSettings` is not enough.

AuthnRequests are not signed (`authenticateRequestSigningBehavior="Never"`), which is acceptable for this local spike. Production should use a signing certificate if Entra requires signed requests.

---

## 5. Runtime request path

1. User opens `https://localhost:44322/Customer/Edit/12345`.
2. The global `SamlAuthorizeAttribute` filter runs. No session cookie → not authenticated.
3. The filter redirects to `/Saml2/SignIn?ReturnUrl=%2FCustomer%2FEdit%2F12345`.
4. Sustainsys builds a SAML AuthnRequest and redirects the browser to `login.microsoftonline.com/{tenant}/saml2?SAMLRequest=...&RelayState=...`.
5. Entra authenticates the user and applies MFA if Conditional Access requires it.
6. Entra POSTs the SAML Response to `https://localhost:44322/Saml2/Acs`.
7. Sustainsys validates signature, audience, issuer, and conditions.
8. `SessionAuthenticationModule` creates the session cookie.
9. The user is redirected to `/Customer/Edit/12345` from RelayState.

If the user already has a valid session, step 1 loads the page immediately. There is no Entra round-trip.

---

## 6. How to run

SAML requires HTTPS. Entra will not post an assertion to an HTTP ACS.

```powershell
& "C:\Program Files\IIS Express\iisexpress.exe" `
  /config:"c:\SMAL Auth\.iisexpress\applicationhost.config" `
  /site:SmalAuth.Saml
```

Then open:

- [https://localhost:44322/Customer/Edit/12345](https://localhost:44322/Customer/Edit/12345) — proves RelayState
- [https://localhost:44322/](https://localhost:44322/) — shows SAML claims after login
- [https://localhost:44322/Saml2/Logout](https://localhost:44322/Saml2/Logout) — signs out

Or press F5 on `SmalAuth.Saml` in Visual Studio.

The original Windows-auth app remains at [http://localhost:5000/](http://localhost:5000/) if that site is started.

---

## 7. Files that implement the migration

| File | Role |
| --- | --- |
| `SmalAuth.Saml\Web.config` | Turns off Windows Auth; Entra / Sustainsys / session cookie |
| `SmalAuth.Saml\App_Start\SamlAuthorizeAttribute.cs` | Global challenge + RelayState / ReturnUrl |
| `SmalAuth.Saml\App_Start\FilterConfig.cs` | Registers `SamlAuthorizeAttribute` once for the whole app |
| `SmalAuth.Saml\App_Start\RouteConfig.cs` | `/Saml2/{action}` |
| `SmalAuth.Saml\App_Start\SamlSettings.cs` | Reads tenant config; setup gate |
| `SmalAuth.Saml\Controllers\HomeController.cs` | Claims page after SAML |
| `SmalAuth.Saml\Controllers\CustomerController.cs` | Deep-link page for RelayState |
| `SmalAuth.Saml\SmalAuth.Saml.csproj` | `Sustainsys.Saml2.Mvc` package |
| `.iisexpress\applicationhost.config` | SAML site on `https://localhost:44322`; Windows Auth only on `SmalAuth` |

---

## 8. Applying this to a real MVC 5 Windows-auth app

Use the same sequence on a production app such as WebPlus:

1. Leave the current Windows-auth site running until SAML is proven.
2. Add Sustainsys.Saml2.Mvc and the `Saml2` route.
3. Switch that environment’s IIS site to Anonymous on, Windows Auth off.
4. Change `authentication mode` from `Windows` to `None`.
5. Register `SamlAuthorizeAttribute` as a **global** filter. Do not stamp it on 400 controllers.
6. Mark only legitimate public endpoints `[AllowAnonymous]`. Leave `/Saml2/*` unchallenged.
7. Register the real SP Entity ID and ACS URL in Entra (not localhost).
8. Point Sustainsys at the Entra federation metadata.
9. Confirm a deep link (`/Customer/Edit/12345` or the real WebPlus URL) returns to the same path after ACS.
10. Search remaining `WindowsIdentity` / `DOMAIN\user` usage and map Azure ID → the existing WebPlus user.
11. Add RSA later only if it is still required after Entra MFA.

---

## 9. Out of scope for this spike

- RSA prompt / RSA value validation (separate path in the WebPlus diagrams)
- AD group gating before issuing an RSA value
- Signed AuthnRequests / production signing certificates
- Switching the original `SmalAuth` Windows-auth project itself over to SAML

The SAML POC proves the authentication path: **who the user is**, via Entra, with RelayState restored. Authorization extras (RSA, groups) can be added after that.
