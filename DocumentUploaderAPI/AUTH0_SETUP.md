# Auth0 Authentication Setup Guide

This API is now protected with Auth0 JWT Bearer authentication. Follow these steps to configure it.

## 1. Auth0 Dashboard Configuration

### Create/Configure an API in Auth0

1. **Log in to Auth0 Dashboard**: https://manage.auth0.com
2. **Navigate to Applications → APIs**
3. **Create a new API** (or use existing):
   - **Name**: `Document Uploader API`
   - **Identifier**: `https://documentuploader-api` (this is your Audience)
   - **Signing Algorithm**: RS256 (recommended)
4. **Copy the Identifier** - you'll need this for appsettings.json

### Configure Your React App in Auth0

Your React app should already be registered. Verify:

1. **Navigate to Applications → Applications**
2. **Find your React app**
3. **Settings tab**:
   - Note your **Domain** (e.g., `your-tenant.auth0.com`)
   - Note your **Client ID**
   - **Allowed Callback URLs**: Add your React app URLs (e.g., `http://localhost:3000/callback`)
   - **Allowed Web Origins**: Add your React app URLs (e.g., `http://localhost:3000`)
   - **Allowed Logout URLs**: Add your React app URLs (e.g., `http://localhost:3000`)

## 2. Configure API Settings

Update `appsettings.json` and `appsettings.Development.json`:

```json
{
  "Auth0": {
    "Domain": "https://your-tenant.auth0.com",
    "Audience": "https://documentuploader-api"
  }
}
```

**Important Notes:**
- **Domain**: Include `https://` prefix (e.g., `https://dev-abc123.us.auth0.com`)
- **Audience**: Use the API Identifier from Auth0 (common format: `https://your-api-identifier`)

## 3. React App Configuration

Your React app needs to:

1. **Request Access Token** with the correct audience
2. **Send JWT in Authorization Header** on API requests

### Example: Using @auth0/auth0-react

```javascript
import { Auth0Provider, useAuth0 } from '@auth0/auth0-react';

// In your App.js
<Auth0Provider
  domain="your-tenant.auth0.com"
  clientId="your-client-id"
  authorizationParams={{
    redirect_uri: window.location.origin,
    audience: "https://documentuploader-api", // IMPORTANT: Must match API Audience
    scope: "openid profile email"
  }}
>
  <YourApp />
</Auth0Provider>

// In your API client/service
const { getAccessTokenSilently } = useAuth0();

async function uploadDocument(file) {
  const token = await getAccessTokenSilently();
  
  const formData = new FormData();
  formData.append('files', file);
  
  const response = await fetch('https://localhost:7190/api/documents/upload', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      // Don't set Content-Type for FormData, browser will set it with boundary
    },
    body: formData
  });
  
  return response.json();
}
```

### Example: Using fetch with manual token

```javascript
// Get token from Auth0
const token = await getAccessTokenSilently();

// Call protected endpoint
const response = await fetch('https://localhost:7190/api/documents', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
```

## 4. Protected Endpoints

The following endpoints require authentication (Bearer token):

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/documents/upload` | Upload documents |
| GET | `/api/documents` | List all documents |
| GET | `/api/documents/{id}` | Get specific document |
| DELETE | `/api/documents/{id}` | Delete document |

**Public endpoints** (no authentication required):
- `GET /` - Service info
- `GET /health` - Health check

## 5. Testing with Authenticated Tokens

### Option 1: Using Scalar UI (Development)

1. **Get an Access Token** from Auth0:
   ```bash
   curl --request POST \
     --url https://your-tenant.auth0.com/oauth/token \
     --header 'content-type: application/json' \
     --data '{
       "client_id": "YOUR_CLIENT_ID",
       "client_secret": "YOUR_CLIENT_SECRET",
       "audience": "https://documentuploader-api",
       "grant_type": "client_credentials"
     }'
   ```

2. **Copy the access_token** from the response

3. **Use in Scalar UI**:
   - Click the lock icon or "Authorize" button
   - Enter: `Bearer YOUR_ACCESS_TOKEN`
   - Or add header manually: `Authorization: Bearer YOUR_ACCESS_TOKEN`

### Option 2: Using curl

```bash
# Set your token
TOKEN="your-jwt-token-here"

# Upload a document
curl -X POST https://localhost:7190/api/documents/upload \
  -H "Authorization: Bearer $TOKEN" \
  -F "files=@/path/to/file.pdf"

# List documents
curl https://localhost:7190/api/documents \
  -H "Authorization: Bearer $TOKEN"

# Get specific document
curl https://localhost:7190/api/documents/{documentId} \
  -H "Authorization: Bearer $TOKEN"

# Delete document
curl -X DELETE https://localhost:7190/api/documents/{documentId} \
  -H "Authorization: Bearer $TOKEN"
```

### Option 3: Using Postman

1. **Create a new request**
2. **Authorization tab**:
   - Type: `OAuth 2.0`
   - Add auth data to: `Request Headers`
   - Configure New Token:
     - Token Name: `Auth0 Token`
     - Grant Type: `Authorization Code (With PKCE)`
     - Callback URL: `https://oauth.pstmn.io/v1/callback`
     - Auth URL: `https://your-tenant.auth0.com/authorize`
     - Access Token URL: `https://your-tenant.auth0.com/oauth/token`
     - Client ID: `YOUR_CLIENT_ID`
     - Scope: `openid profile email`
     - Audience: `https://documentuploader-api`
3. **Get New Access Token**
4. **Use Token**

## 6. Common Issues

### 401 Unauthorized

**Cause**: Token is missing, invalid, or expired

**Solutions**:
- Verify token is being sent in `Authorization: Bearer <token>` header
- Check token hasn't expired (default: 24 hours)
- Verify `Audience` matches in React app and API configuration
- Check `Domain` is correct in both places

### "The audience is invalid"

**Cause**: Token audience doesn't match API configuration

**Solutions**:
- Verify `audience` parameter in React Auth0Provider matches API's `Auth0:Audience`
- Ensure you're passing `audience` when requesting the token

### "IDX10205: Issuer validation failed"

**Cause**: Domain configuration mismatch

**Solutions**:
- Verify `Auth0:Domain` in appsettings.json includes `https://`
- Check domain matches your Auth0 tenant exactly

### CORS Errors

**Cause**: React app origin not allowed

**Solutions**:
- Current API allows all origins (`.AllowAnyOrigin()`)
- For production, restrict to specific origins:
  ```csharp
  app.UseCors(x => x
      .WithOrigins("https://yourdomain.com")
      .AllowAnyMethod()
      .AllowAnyHeader());
  ```

## 7. Security Best Practices

1. **Use Environment Variables** for sensitive values in production
2. **Restrict CORS** to specific origins in production
3. **Enable HTTPS** always (already configured)
4. **Token Expiration**: Auth0 default is 24 hours - adjust if needed
5. **Scopes/Permissions**: Consider adding scopes for fine-grained access control
6. **Rate Limiting**: Consider adding rate limiting middleware

## 8. Additional Resources

- [Auth0 ASP.NET Core Web API Quickstart](https://auth0.com/docs/quickstart/backend/aspnet-core-webapi)
- [Auth0 React SDK Quickstart](https://auth0.com/docs/quickstart/spa/react)
- [Auth0 API Authorization](https://auth0.com/docs/secure/tokens/access-tokens/get-access-tokens)
