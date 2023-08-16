# LICENSING & ANALYTICS FOR SOFTWARE AND IoT VENDORS

Instead of using the official [SLASCONE NuGet package](https://www.nuget.org/profiles/SLASCONE), this example consumes the SLASCONE API directly. 

In most cases, it is strongly recommended to use the NuGet package as demonstrated in the following GitHub repositories:

- [SLASCONE Demo (console application)](https://github.com/SLASCONE/SLASCONE-demo-csharp-nuget)
- [SLASCONE Demo (desktop/wpf application)](https://github.com/SLASCONE/SLASCONE-demo-wpf-nuget)

## Floating

Regardless of the usage of the NuGet package or not, this examples focuses on floating license scenarios.
You can read more about our floating license functionality [here.](https://support.slascone.com/hc/en-us/articles/360016152858-FLOATING-DEVICE-LICENSES)

## Client ID

In this example the unique identifier of the Windows system is used as a unique client id.

```
public static string GetWindowsUniqueDeviceId()
{
    using (var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
	{
		var shares = searcher.Get();
		var props = shares.Cast<ManagementObject>().First().Properties;
		var uuid = props["UUID"].Value as string;
		return uuid;
	}
}
```

You can find more infos about unique device id's in the article "[Generating a unique client id](https://support.slascone.com/hc/en-us/articles/360016157958-GENERATING-A-UNIQUE-CLIENT-ID)".

## Workflow

The license check is performed according to the following flowchart:

![License check](./images/licensecheck.png)

The first step is to create a license heartbeat.
- If the heartbeat succeeds, the license check is already done. The license info and the signature can be stored in 
local files in case of possible communication problems in the future. (such as no internet connection)
- If the license heartbeat fails, different cases must be distinguished based on the error code.  
  - Error code *2006* means that the device id is unknown to the slascone server. The next step then is to activate the license for the device.
  -  Other errors indicate a problem with the license.
  -  Problems communicating with the server can be dealt with using a special offline mode.

## Session handling

Opening, renewing and closing sessions is described in the article "[Floating device licenses](https://support.slascone.com/hc/en-us/articles/360016152858-FLOATING-DEVICE-LICENSES)"

## Handling communication problems

The license information can be saved locally on the device to enable work despite a missing connection to the Slascone server.
See the article 
"[What and how to save in your client](https://support.slascone.com/hc/en-us/articles/7702036319261-WHAT-AND-HOW-TO-SAVE-IN-YOUR-CLIENT)".

The manipulation of locally stored license information can be prevented by storing the signature provided by the Slascone server locally and validating the license information with it and with the public key provided.

See more information in the article "[Digital signature and data integrity](https://support.slascone.com/hc/en-us/articles/360016063637-DIGITAL-SIGNATURE-AND-DATA-INTEGRITY)"

## Client generation with NSwagStudio

This demo uses a Slascone client generated with NSwagStudio.

### Settings for client generation

Set the .NET runtime to "Net60" ad load the OpenAPI specification from [https://api.slascone.com/swagger/v2/]

![Choose .NET](./images/netframework.png)

#### CSharp Client Settings

The following settings differing from the default settings were used
to generate the client.

- Namespace: `SlasconeClient`
- Operation Generation Mode: _MultipleClientsForPathSegments_
- Activate the setting "_Generate PrepareRequest and ProcessResponse as asynchronous methods_"  
This setting is necessary to get access to the body of the Http response. In order for the comparison with the signature to work, the exact byte image of the server response is required.
- Response Wrapping  
With some methods, the error number of the Slascone server is required in order to be able to recognize certain situations.
These methods are enumerated so that the result is wrapped in a SwaggerResponse that provides the error number of the Slascone server.
For more information on this topic, see the article about "[Error codes](https://support.slascone.com/hc/en-us/articles/360016160398-ERROR-CODES)"  
![Response Wrapping](./images/responsewrapping.png)
