workspace "miditobms"
	configurations { "Debug", "Release" }
	targetdir "bin/%{cfg.buildcfg}"
	libdirs { "lib" }
	
	startproject "miditobms"
	
	filter "configurations:Debug"
		defines { "DEBUG" }
		flags { "Symbols" }
	
	filter "configurations:Release"
		defines { "RELEASE" }
		optimize "On"
	
	project "miditobms"
		kind "SharedLib"
		language "C#"
		namespace "arookas"
		location "miditobms"
		
		links { "System", "arookas", }
		
		files {
			"miditobms/**.cs",
		}
		
		excludes {
			"miditobms/bin/**",
			"miditobms/obj/**",
		}
		
