#!/usr/bin/env ruby
# Usage: ruby integrate_host.rb /path/to/Unity-visionOS-build /path/to/NativeHost/Sources [debug|production]
# Runs once on a COPY of a fresh Unity Xcode build. Refuses an already integrated target.
require 'xcodeproj'
require 'fileutils'
require 'pathname'

build_dir, source_dir, mode = ARGV
mode ||= 'debug'
abort 'Mode must be debug or production' unless %w[debug production].include?(mode)
abort 'Pass Unity build directory and NativeHost/Sources directory' unless build_dir && source_dir
project_path = Dir[File.join(build_dir, '*.xcodeproj')].first or abort 'No .xcodeproj found'
project = Xcodeproj::Project.open(project_path)
abort 'NativeHost target already exists' if project.targets.any? { |target| target.name == 'NativeHost' }
unity_app = project.targets.find { |target| target.product_type == 'com.apple.product-type.application' }
unity_framework = project.targets.find { |target| target.name == 'UnityFramework' }
abort 'Expected Unity app and UnityFramework targets' unless unity_app && unity_framework

host_files = %w[Bridge.swift HostApp.swift UnityRuntime.swift ModelAssetStore.swift AudioAssetStore.swift SensorService.swift AnchorService.swift MapService.swift]
host_folder = File.join(build_dir, 'NativeHost')
FileUtils.mkdir_p(host_folder)
host_files.each do |filename|
  FileUtils.cp(File.join(source_dir, filename), File.join(host_folder, filename))
end
File.write(File.join(host_folder, 'Info.plist'), <<~PLIST)
  <?xml version="1.0" encoding="UTF-8"?>
  <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
  <plist version="1.0"><dict>
    <key>CFBundleDisplayName</key><string>Unity RealityKit MVP</string>
    <key>CFBundleExecutable</key><string>$(EXECUTABLE_NAME)</string>
    <key>CFBundleIdentifier</key><string>$(PRODUCT_BUNDLE_IDENTIFIER)</string>
    <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
    <key>CFBundleName</key><string>$(PRODUCT_NAME)</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleShortVersionString</key><string>1.0</string>
    <key>CFBundleVersion</key><string>1</string>
    <key>MVPLogicOnly</key><true/>
    <key>MVPDebugUI</key><#{mode == 'debug'}/>
    <key>NSHandsTrackingUsageDescription</key><string>Usamos las manos para que puedas programar interacciones con objetos inmersivos desde Unity.</string>
    <key>NSWorldSensingUsageDescription</key><string>Detectamos superficies para colocar objetos de tu experiencia en la habitación.</string>
    <key>UIApplicationSceneManifest</key><dict><key>UIApplicationSupportsMultipleScenes</key><true/></dict>
  </dict></plist>
PLIST

host = project.new_target(:application, 'NativeHost', :visionos, '1.0')
group = project.main_group.new_group('NativeHost', 'NativeHost')
host_files.each do |filename|
  ref = group.new_file(filename)
  host.source_build_phase.add_file_reference(ref)
end
audio_dir = File.join(build_dir, 'VisionAudio')
if Dir.exist?(audio_dir) && !Dir[File.join(audio_dir, '*.{wav,aif,aiff}')].empty?
  audio_ref = project.main_group.new_file('VisionAudio')
  audio_ref.last_known_file_type = 'folder'
  host.resources_build_phase.add_file_reference(audio_ref)
end
group.new_file('Info.plist')
host.add_dependency(unity_framework)
# Link the framework and embed the built product into the native app.
framework_ref = unity_framework.product_reference
host.frameworks_build_phase.add_file_reference(framework_ref)
embed = host.new_copy_files_build_phase('Embed UnityFramework')
embed.dst_subfolder_spec = '10' # Frameworks
item = embed.add_file_reference(framework_ref)
item.settings = { 'ATTRIBUTES' => ['CodeSignOnCopy', 'RemoveHeadersOnCopy'] }
# Unity's build stores Data in the application bundle, not in UnityFramework.
data_ref = project.files.find { |file| file.path == 'Data' }
abort 'Missing Unity Data folder reference' unless data_ref
host.resources_build_phase.add_file_reference(data_ref)
# The Unity Editor exporter places USDA models beside the Xcode project.
# A folder reference preserves the VisionModels subdirectory in the app.
models_dir = File.join(build_dir, 'VisionModels')
if Dir.exist?(models_dir) && !Dir[File.join(models_dir, '*.usda')].empty?
  models_ref = project.main_group.new_file('VisionModels')
  models_ref.last_known_file_type = 'folder'
  host.resources_build_phase.add_file_reference(models_ref)
end

host.build_configurations.each do |config|
  settings = config.build_settings
  unity_config = unity_app.build_configurations.find { |candidate| candidate.name == config.name }
  unity_settings = unity_config&.build_settings || {}
  settings['SDKROOT'] = 'xros'
  settings['SUPPORTED_PLATFORMS'] = 'xros xrsimulator'
  settings['TARGETED_DEVICE_FAMILY'] = '7'
  settings['XROS_DEPLOYMENT_TARGET'] = '2.0'
  settings['SWIFT_VERSION'] = '5.0'
  settings['INFOPLIST_FILE'] = 'NativeHost/Info.plist'
  # Unity Player Settings are the source of truth for signing. A developer
  # chooses team and bundle ID once in Unity; the host inherits both.
  settings['PRODUCT_BUNDLE_IDENTIFIER'] = unity_settings['PRODUCT_BUNDLE_IDENTIFIER'] || 'com.example.UnityRealityKitMVP'
  settings['DEVELOPMENT_TEAM'] = unity_settings['DEVELOPMENT_TEAM'] if unity_settings.key?('DEVELOPMENT_TEAM')
  settings['PRODUCT_NAME'] = 'NativeHost'
  settings['LD_RUNPATH_SEARCH_PATHS'] = '$(inherited) @executable_path/Frameworks'
  settings['GENERATE_INFOPLIST_FILE'] = 'NO'
  settings['CODE_SIGN_STYLE'] = unity_settings['CODE_SIGN_STYLE'] || 'Automatic'
  settings['ENABLE_USER_SCRIPT_SANDBOXING'] = 'NO'
end
project.save
# Unity's runEmbedded path makes its UIWindow key during initialization.
# Guard those calls in this exported build so they cannot replace SwiftUI's
# ImmersiveSpace before the host has a chance to hide the window.
{
  'Classes/main.mm' => '    [[[self appController] window] makeKeyAndVisible];',
  'Classes/UI/UnityAppController+ViewHandling.mm' => '    [_window makeKeyAndVisible];'
}.each do |relative_path, call|
  path = File.join(build_dir, relative_path)
  source = File.read(path)
  abort "Expected Unity window call missing in #{path}" unless source.include?(call)
  guard = "    if (![[[NSBundle mainBundle] objectForInfoDictionaryKey:@\"MVPLogicOnly\"] boolValue])\n        #{call.strip}"
  source = source.gsub(call, guard)
  File.write(path, source)
end
puts "Added NativeHost (#{mode}) to #{project_path}"
