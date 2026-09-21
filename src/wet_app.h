#pragma once

#include <rex/rex_app.h>
#include <rex/cvar.h>
#include <rex/filesystem.h>
#include <rex/ppc/context.h>
#include <rex/system/function_dispatcher.h>
#include <rex/ui/keybinds.h>
#include <rex/ui/imgui_dialog.h>
#include <rex/logging/macros.h>

#include "display_refresh.h"
#include "wet_menu.h"

#include <filesystem>
#include <memory>

REXCVAR_DEFINE_BOOL(show_fps_overlay, false, "UI",
                    "Show FPS overlay");

static void MissingGuestFunctionStub(PPCContext& ctx, uint8_t*) {
  REXLOG_ERROR("Unregistered guest call 0x{:08X}; returning",
               ctx.last_indirect_target);
}

class WetApp : public rex::ReXApp {
 public:
  using rex::ReXApp::ReXApp;

  static std::unique_ptr<rex::ui::WindowedApp> Create(
      rex::ui::WindowedAppContext& ctx) {
    return std::unique_ptr<WetApp>(new WetApp(ctx, "wet", PPCImageConfig));
  }

  void OnConfigurePaths(rex::PathConfig& paths) override {
    if (!paths.game_data_root.empty()) return;
    const std::filesystem::path candidates[] = {
        rex::filesystem::GetExecutableFolder() / "game",
        std::filesystem::current_path() / "game",
    };
    for (const auto& candidate : candidates) {
      std::error_code ec;
      if (std::filesystem::is_directory(candidate, ec)) {
        paths.game_data_root = candidate;
        return;
      }
    }
  }

  void OnPreSetup(rex::RuntimeConfig& config) override {
    config.gpu_plugin = "xenos";
    rex::cvar::SetFlagByName("input_backend", "sdl");
    auto keybind_default = [](const char* name, const char* value) {
      if (rex::cvar::GetFlagSource(name) == rex::cvar::Source::kDefault) {
        rex::cvar::SetFlagByName(name, value);
      }
    };
    keybind_default("mnk_mode", "true");
    keybind_default("mnk_mouse", "true");
    keybind_default("mnk_sensitivity", "2.0");
    keybind_default("keybind_a", "Space");
    keybind_default("keybind_b", "F");
    keybind_default("keybind_x", "C");
    keybind_default("keybind_y", "E");
    keybind_default("keybind_left_shoulder", "Q");
    keybind_default("keybind_right_shoulder", "RMB");
    keybind_default("keybind_left_trigger", "LMB");
    keybind_default("keybind_right_trigger", "LMB");
    keybind_default("keybind_lstick_up", "W");
    keybind_default("keybind_lstick_down", "S");
    keybind_default("keybind_lstick_left", "A");
    keybind_default("keybind_lstick_right", "D");
    keybind_default("keybind_lstick_press", "X");
    keybind_default("keybind_rstick_up", "Up");
    keybind_default("keybind_rstick_down", "Down");
    keybind_default("keybind_rstick_left", "Left");
    keybind_default("keybind_rstick_right", "Right");
    keybind_default("keybind_rstick_press", "R");
    keybind_default("keybind_dpad_up", "Shift+Up");
    keybind_default("keybind_dpad_down", "Shift+Down");
    keybind_default("keybind_dpad_left", "Shift+Left");
    keybind_default("keybind_dpad_right", "Shift+Right");
    keybind_default("keybind_back", "Tab");
    keybind_default("keybind_start", "Escape");
    wet::ApplyDefaultGraphicsQuality();
    wet::ApplyFrameRatePolicy();
  }

  void OnPreLaunchModule() override {
    auto* dispatcher = runtime()->function_dispatcher();
    if (!dispatcher) return;
    constexpr uint32_t kMissing[] = {0x83347B48, 0x83356DB8, 0x82A234A0, 0x828D44E0};
    for (uint32_t addr : kMissing) {
      if (dispatcher->GetFunction(addr)) continue;
      dispatcher->SetFunction(addr, &MissingGuestFunctionStub);
      REXLOG_WARN("Registered stub for missing guest function 0x{:08X}", addr);
    }
  }

  void OnPostSetup() override {
    if (imgui_drawer()) {
      settings_menu_ = std::make_unique<WetSettingsMenu>(imgui_drawer());
    }
    rex::ui::RegisterBind("bind_settings_menu", "F3", "Toggle settings", [this] {
      if (settings_menu_) settings_menu_->Toggle();
    });
    rex::ui::RegisterBind("bind_exit_game", "Alt+F4", "Exit", [this] {
      app_context().RequestDeferredQuit();
    });
  }

  void OnShutdown() override {
    rex::ui::UnregisterBind("bind_settings_menu");
    rex::ui::UnregisterBind("bind_exit_game");
    settings_menu_.reset();
  }

 private:
  std::unique_ptr<WetSettingsMenu> settings_menu_;
};
