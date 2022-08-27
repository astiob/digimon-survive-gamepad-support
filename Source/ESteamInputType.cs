namespace GamepadSupportPlugin
{
	enum ESteamInputType
	{
		k_ESteamInputType_Unknown,
		k_ESteamInputType_SteamController,
		k_ESteamInputType_XBox360Controller,
		k_ESteamInputType_XBoxOneController,
		k_ESteamInputType_GenericGamepad,       // DirectInput controllers
		k_ESteamInputType_PS4Controller,
		k_ESteamInputType_AppleMFiController,   // Unused
		k_ESteamInputType_AndroidController,    // Unused
		k_ESteamInputType_SwitchJoyConPair,     // Unused
		k_ESteamInputType_SwitchJoyConSingle,   // Unused
		k_ESteamInputType_SwitchProController,
		k_ESteamInputType_MobileTouch,          // Steam Link App On-screen Virtual Controller
		k_ESteamInputType_PS3Controller,        // Currently uses PS4 Origins
		k_ESteamInputType_PS5Controller,        // Added in SDK 151
		k_ESteamInputType_SteamDeckController,  // Added in SDK 153
		k_ESteamInputType_Count,
		k_ESteamInputType_MaximumPossibleValue = 255,
	}
}
