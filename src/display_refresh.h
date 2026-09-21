#pragma once

#include <rex/cvar.h>
#include <rex/logging/macros.h>

#include <algorithm>
#include <cmath>
#include <string>

#ifdef _WIN32
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#endif

REXCVAR_DEFINE_STRING(framerate_mode, "auto", "Graphics",
                      "Frame pacing: auto (match display Hz), 60, 120, 144, "
                      "165, 240, or unlocked");

namespace wet {

inline double DetectDisplayRefreshHz() {
#ifdef _WIN32
  DEVMODEW mode{};
  mode.dmSize = sizeof(mode);
  if (EnumDisplaySettingsW(nullptr, ENUM_CURRENT_SETTINGS, &mode) &&
      mode.dmDisplayFrequency > 1 && mode.dmDisplayFrequency < 1000) {
    return static_cast<double>(mode.dmDisplayFrequency);
  }
#endif
  return 60.0;
}

inline double SanitizeRefreshHz(double hz) {
  if (!(hz > 0.0) || !std::isfinite(hz)) return 60.0;
  return std::clamp(hz, 30.0, 360.0);
}

inline double ResolveFrameRateHz(const std::string& mode, double detected_hz) {
  const double display_hz = SanitizeRefreshHz(detected_hz);
  if (mode == "60") return 60.0;
  if (mode == "120") return 120.0;
  if (mode == "144") return 144.0;
  if (mode == "165") return 165.0;
  if (mode == "240") return 240.0;
  if (mode == "unlocked") return display_hz;
  return display_hz;
}

inline void ApplyFrameRatePolicy() {
  const std::string mode = REXCVAR_GET(framerate_mode);
  const double detected = DetectDisplayRefreshHz();
  const double hz = ResolveFrameRateHz(mode, detected);
  const bool vsync = mode != "unlocked";

  if (mode == "auto" || mode == "unlocked") {
    rex::cvar::SetFlagByName("video_mode_refresh_rate", std::to_string(hz));
    rex::cvar::SetFlagByName("target_fps",
                             mode == "unlocked" ? "0" : std::to_string(hz));
    rex::cvar::SetFlagByName("vsync", vsync ? "true" : "false");
    rex::cvar::SetFlagByName("d3d12_host_vsync", vsync ? "true" : "false");
  } else {
    auto set_if_default = [](const char* name, const std::string& value) {
      if (rex::cvar::GetFlagSource(name) == rex::cvar::Source::kDefault) {
        rex::cvar::SetFlagByName(name, value);
      }
    };
    set_if_default("video_mode_refresh_rate", std::to_string(hz));
    set_if_default("target_fps", std::to_string(hz));
    set_if_default("vsync", vsync ? "true" : "false");
    set_if_default("d3d12_host_vsync", vsync ? "true" : "false");
  }

  rex::cvar::SetFlagByName("d3d12_allow_variable_refresh_rate_and_tearing", "true");

  REXLOG_INFO("FRAME PACE: mode={} display={:.0f} Hz target={:.0f} Hz vsync={}",
              mode, detected, hz, vsync);
}

inline void ApplyDefaultGraphicsQuality() {
  auto set_if_default = [](const char* name, const char* value) {
    if (rex::cvar::GetFlagSource(name) == rex::cvar::Source::kDefault) {
      rex::cvar::SetFlagByName(name, value);
    }
  };
  set_if_default("anisotropic_override", "16");
  set_if_default("swap_post_effect", "fxaa");
  set_if_default("present_effect", "bilinear");
  set_if_default("vsync", "true");
  set_if_default("d3d12_host_vsync", "true");
  set_if_default("d3d12_allow_variable_refresh_rate_and_tearing", "true");
  set_if_default("fullscreen", "true");
  set_if_default("resolution", "1080p");
  set_if_default("window_width", "1920");
  set_if_default("window_height", "1080");
}

}  // namespace wet
