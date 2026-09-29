#pragma once

#include <rex/cvar.h>
#include <rex/ui/imgui_dialog.h>
#include <imgui.h>

#include "display_refresh.h"

#include <cstdio>
#include <string>

class WetSettingsMenu : public rex::ui::ImGuiDialog {
 public:
  explicit WetSettingsMenu(rex::ui::ImGuiDrawer* drawer)
      : rex::ui::ImGuiDialog(drawer) {
    SyncFromCvars();
  }

  void Toggle() { open_ = !open_; }

 protected:
  void OnDraw(ImGuiIO&) override {
    if (!open_) return;

    ImGui::PushStyleColor(ImGuiCol_WindowBg, ImVec4(0.03f, 0.08f, 0.12f, 0.96f));
    ImGui::PushStyleColor(ImGuiCol_Border, ImVec4(0.20f, 0.70f, 0.82f, 1.0f));
    ImGui::PushStyleColor(ImGuiCol_Button, ImVec4(0.08f, 0.45f, 0.55f, 1.0f));
    ImGui::PushStyleColor(ImGuiCol_ButtonHovered, ImVec4(0.16f, 0.70f, 0.82f, 1.0f));
    ImGui::PushStyleColor(ImGuiCol_FrameBg, ImVec4(0.05f, 0.16f, 0.22f, 1.0f));
    ImGui::PushStyleColor(ImGuiCol_Header, ImVec4(0.08f, 0.40f, 0.50f, 1.0f));
    ImGui::PushStyleColor(ImGuiCol_Tab, ImVec4(0.06f, 0.28f, 0.36f, 1.0f));
    ImGui::PushStyleColor(ImGuiCol_TabSelected, ImVec4(0.12f, 0.55f, 0.68f, 1.0f));
    ImGui::PushStyleColor(ImGuiCol_Text, ImVec4(0.85f, 0.95f, 0.98f, 1.0f));
    ImGui::PushStyleVar(ImGuiStyleVar_WindowRounding, 8.0f);

    ImGui::SetNextWindowSize(ImVec2(560, 700), ImGuiCond_Always);
    bool visible = true;
    if (ImGui::Begin("##wet_settings", &visible,
                     ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags_NoResize |
                         ImGuiWindowFlags_NoTitleBar)) {
      ImGui::PushStyleColor(ImGuiCol_Text, ImVec4(0.35f, 0.90f, 1.0f, 1.0f));
      ImGui::SetWindowFontScale(1.4f);
      ImGui::TextUnformatted("WET");
      ImGui::SetWindowFontScale(1.0f);
      ImGui::PopStyleColor();
      ImGui::TextUnformatted("SETTINGS");
      ImGui::TextUnformatted("Ported by Mohammed Albarghouthi");
      ImGui::Separator();
      ImGui::Spacing();

      if (ImGui::BeginTabBar("wet_settings_tabs")) {
        if (ImGui::BeginTabItem("Display")) {
          DrawDisplay();
          ImGui::EndTabItem();
        }
        if (ImGui::BeginTabItem("Graphics")) {
          DrawGraphics();
          ImGui::EndTabItem();
        }
        if (ImGui::BeginTabItem("Controls")) {
          DrawControls();
          ImGui::EndTabItem();
        }
        ImGui::EndTabBar();
      }

      ImGui::Spacing();
      if (status_[0] != '\0') {
        ImGui::TextWrapped("%s", status_);
        ImGui::Spacing();
      }

      if (ImGui::Button("Apply", ImVec2(140, 34))) Apply();
      ImGui::SameLine();
      if (ImGui::Button("Close", ImVec2(140, 34))) open_ = false;
      ImGui::TextUnformatted("F3 close  ·  F1 FPS overlay  ·  restart for resolution");
    }
    ImGui::End();
    ImGui::PopStyleVar();
    ImGui::PopStyleColor(9);
    if (!visible) open_ = false;
  }

 private:
  void DrawDisplay() {
    ImGui::Combo("Resolution", &resolution_,
                 "Auto (match monitor)\01080p\02K (1440p)\0"
                 "Ultrawide 2560x1080\0Ultrawide 3440x1440\04K\0");
    ImGui::Combo("Language", &language_,
                 "English\0Japanese\0German\0French\0Spanish\0Italian\0"
                 "Korean\0Chinese (Trad.)\0Portuguese\0Chinese (Simp.)\0"
                 "Polish\0Russian\0");
    ImGui::Combo("Frame pace", &fps_mode_,
                 "Match Display\060 Hz\0120 Hz\0144 Hz\0Unlocked\0");
    ImGui::Checkbox("VSync", &vsync_);
    ImGui::SameLine(0, 24);
    ImGui::Checkbox("Fullscreen", &fullscreen_);
    ImGui::Checkbox("FPS overlay", &fps_overlay_);
    ImGui::Text("Display: %.0f Hz", wet::DetectDisplayRefreshHz());
    ImGui::TextWrapped(
        "Ultrawide fills 21:9. Language and resolution apply after restart.");
  }

  void DrawGraphics() {
    if (ImGui::Combo("Quality", &preset_,
                     "Performance\0Balanced\0Quality\0Ultra\0")) {
      FillPreset(preset_);
    }
    ImGui::Combo("Renderer", &gpu_backend_, "Auto\0Direct3D 12\0Vulkan\0");
    ImGui::Combo("Render scale", &scale_, "Native (1x)\0High (2x)\0Ultra (3x)\0");
    ImGui::Combo("Anti-aliasing", &aa_, "Off\0FXAA\0FXAA Extreme\0");
    ImGui::Combo("Filtering", &aniso_, "Default\04x\08x\016x\0");
    ImGui::Combo("Sharpen", &post_, "Off\0On\0");
    ImGui::TextWrapped(
        "Render scale redraws the game above Xbox 360 resolution. "
        "2x is the usual upgrade. 3x is capped at 1080p.");
  }

  void DrawControls() {
    ImGui::Checkbox("SDL controller (Xbox / PlayStation / Switch)", &sdl_);
    ImGui::Checkbox("Keyboard and mouse as a controller", &mnk_);
    ImGui::Checkbox("Mouse looks (right stick)", &mouse_look_);
    ImGui::SliderFloat("Mouse sensitivity", &sensitivity_, 0.5f, 8.0f, "%.1f");
    ImGui::TextWrapped(
        "WASD moves, mouse looks. Left click shoots, right click sword. "
        "Rebinds are in the launcher CONTROLS tab and apply on PLAY.");
  }

  void FillPreset(int preset) {
    switch (preset) {
      case 0:
        scale_ = 0;
        aa_ = 0;
        aniso_ = 1;
        post_ = 0;
        break;
      case 1:
        scale_ = 0;
        aa_ = 1;
        aniso_ = 3;
        post_ = 0;
        break;
      case 2:
        scale_ = 1;
        aa_ = 2;
        aniso_ = 3;
        post_ = 1;
        break;
      default:
        scale_ = resolution_ <= 1 ? 2 : 1;
        aa_ = 2;
        aniso_ = 3;
        post_ = 1;
        break;
    }
  }

  void ApplyResolution() {
    const char* presets[] = {"", "1080p", "1440p", "2560x1080", "3440x1440", "4k"};
    const char* widths[] = {"0", "1920", "2560", "2560", "3440", "3840"};
    const char* heights[] = {"0", "1080", "1440", "1080", "1440", "2160"};
    if (resolution_ <= 0) return;
    rex::cvar::SetFlagByName("resolution", presets[resolution_]);
    rex::cvar::SetFlagByName("window_width", widths[resolution_]);
    rex::cvar::SetFlagByName("window_height", heights[resolution_]);
    rex::cvar::SetFlagByName("video_mode_width", widths[resolution_]);
    rex::cvar::SetFlagByName("video_mode_height", heights[resolution_]);
    const bool ultrawide = resolution_ == 3 || resolution_ == 4;
    rex::cvar::SetFlagByName("present_letterbox", ultrawide ? "false" : "true");
  }

  void ApplyScale() {
    const char* scales[] = {"1", "2", "3"};
    int scale = scale_;
    if (resolution_ >= 2 && scale > 1) scale = 1;
    rex::cvar::SetFlagByName("resolution_scale", scales[scale]);
    rex::cvar::SetFlagByName("draw_resolution_scale_x", scales[scale]);
    rex::cvar::SetFlagByName("draw_resolution_scale_y", scales[scale]);
  }

  void Apply() {
    const char* aa[] = {"none", "fxaa", "fxaa_extreme"};
    const char* aniso[] = {"-1", "3", "4", "5"};
    const char* modes[] = {"auto", "60", "120", "144", "unlocked"};
    rex::cvar::SetFlagByName("swap_post_effect", aa[aa_]);
    rex::cvar::SetFlagByName("anisotropic_override", aniso[aniso_]);
    rex::cvar::SetFlagByName("present_effect", "bilinear");
    rex::cvar::SetFlagByName("present_dither", post_ > 0 ? "true" : "false");
    rex::cvar::SetFlagByName("native_2x_msaa", "true");
    rex::cvar::SetFlagByName("framerate_mode", modes[fps_mode_]);
    rex::cvar::SetFlagByName("fullscreen", fullscreen_ ? "true" : "false");
    rex::cvar::SetFlagByName("show_fps_overlay", fps_overlay_ ? "true" : "false");
    rex::cvar::SetFlagByName("vsync", vsync_ && fps_mode_ != 4 ? "true" : "false");
    rex::cvar::SetFlagByName("d3d12_host_vsync",
                             vsync_ && fps_mode_ != 4 ? "true" : "false");
    const double hz = wet::ResolveFrameRateHz(modes[fps_mode_],
                                              wet::DetectDisplayRefreshHz());
    rex::cvar::SetFlagByName("video_mode_refresh_rate", std::to_string(hz));
    rex::cvar::SetFlagByName("target_fps",
                             fps_mode_ == 4 ? "0" : std::to_string(hz));
    rex::cvar::SetFlagByName("input_backend", sdl_ ? "sdl" : "xinput");
    rex::cvar::SetFlagByName("mnk_mode", mnk_ ? "true" : "false");
    rex::cvar::SetFlagByName("mnk_mouse", mouse_look_ ? "true" : "false");
    char sens[16];
    std::snprintf(sens, sizeof(sens), "%.1f", sensitivity_);
    rex::cvar::SetFlagByName("mnk_sensitivity", sens);
    const char* gpus[] = {"any", "d3d12", "vulkan"};
    rex::cvar::SetFlagByName("gpu_backend", gpus[gpu_backend_]);
    char lang[8];
    std::snprintf(lang, sizeof(lang), "%d", language_ + 1);
    rex::cvar::SetFlagByName("user_language", lang);
    ApplyResolution();
    ApplyScale();
    std::snprintf(status_, sizeof(status_),
                  "Applied. Resolution, render scale, and frame pace need a restart.");
  }

  void SyncFromCvars() {
    const std::string res = SafeQueryString("resolution", "");
    if (res == "1080p")
      resolution_ = 1;
    else if (res == "1440p")
      resolution_ = 2;
    else if (res == "2560x1080")
      resolution_ = 3;
    else if (res == "3440x1440")
      resolution_ = 4;
    else if (res == "4k" || res == "2160p")
      resolution_ = 5;
    else
      resolution_ = 0;

    const std::string aa = SafeQueryString("swap_post_effect", "fxaa_extreme");
    if (aa == "fxaa_extreme")
      aa_ = 2;
    else if (aa == "fxaa")
      aa_ = 1;
    else
      aa_ = 0;

    const int aniso = SafeQueryInt("anisotropic_override", 5);
    if (aniso >= 5)
      aniso_ = 3;
    else if (aniso == 4)
      aniso_ = 2;
    else if (aniso >= 1)
      aniso_ = 1;
    else
      aniso_ = 0;

    const int scale = SafeQueryInt("resolution_scale", 2);
    if (scale >= 3)
      scale_ = 2;
    else if (scale >= 2)
      scale_ = 1;
    else
      scale_ = 0;

    const std::string mode = REXCVAR_GET(framerate_mode);
    if (mode == "60")
      fps_mode_ = 1;
    else if (mode == "120")
      fps_mode_ = 2;
    else if (mode == "144")
      fps_mode_ = 3;
    else if (mode == "unlocked")
      fps_mode_ = 4;
    else
      fps_mode_ = 0;

    fullscreen_ = SafeQueryBool("fullscreen", true);
    vsync_ = SafeQueryBool("vsync", true);
    fps_overlay_ = SafeQueryBool("show_fps_overlay", false);
    post_ = SafeQueryBool("present_dither", false) ? 1 : 0;
    sdl_ = SafeQueryString("input_backend", "sdl") != "xinput";
    mnk_ = SafeQueryBool("mnk_mode", true);
    mouse_look_ = SafeQueryBool("mnk_mouse", true);
    language_ = SafeQueryInt("user_language", 1) - 1;
    if (language_ < 0 || language_ > 11) language_ = 0;
    const std::string gpu = SafeQueryString("gpu_backend", "any");
    if (gpu == "d3d12")
      gpu_backend_ = 1;
    else if (gpu == "vulkan")
      gpu_backend_ = 2;
    else
      gpu_backend_ = 0;
    try {
      sensitivity_ = static_cast<float>(rex::cvar::Query<double>("mnk_sensitivity"));
    } catch (...) {
      sensitivity_ = 2.0f;
    }
  }

  static std::string SafeQueryString(const char* name, const char* fallback) {
    try {
      return rex::cvar::Query<std::string>(name);
    } catch (...) {
      return fallback;
    }
  }

  static int SafeQueryInt(const char* name, int fallback) {
    try {
      return rex::cvar::Query<int32_t>(name);
    } catch (...) {
      return fallback;
    }
  }

  static bool SafeQueryBool(const char* name, bool fallback) {
    try {
      return rex::cvar::Query<bool>(name);
    } catch (...) {
      return fallback;
    }
  }

  bool open_ = false;
  int preset_ = 2;
  int resolution_ = 1;
  int scale_ = 1;
  int aa_ = 2;
  int aniso_ = 3;
  int post_ = 1;
  int fps_mode_ = 0;
  bool vsync_ = true;
  bool fullscreen_ = true;
  bool fps_overlay_ = false;
  bool sdl_ = true;
  bool mnk_ = true;
  bool mouse_look_ = true;
  int language_ = 0;
  int gpu_backend_ = 0;
  float sensitivity_ = 2.0f;
  char status_[192] = {};
};
