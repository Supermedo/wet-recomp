#pragma once

#include <rex/cvar.h>
#include <rex/logging/macros.h>
#include <rex/ppc/func.h>
#include <rex/system/function_dispatcher.h>

#include <cstdint>

// Xbox 360 D3D CreateDevice (sub_83074148) copies X_VIDEO_MODE into the
// device, then creates the backbuffer from D3DPRESENT_PARAMETERS. WET fills
// those params with a 16:9 size, so a 2560x1080 window still swaps 16:9 and
// ReXGlue stretches it. Patch the present params before CreateDevice so the
// renderer matches video_mode (Hor+ if the title uses width/height for FOV).
namespace wet {
namespace {

PPCFunc* g_d3d_create_device = nullptr;

void StoreGuestU32(uint8_t* base, uint32_t ea, uint32_t value) {
  uint8_t* p = base + ea;
  p[0] = uint8_t(value >> 24);
  p[1] = uint8_t(value >> 16);
  p[2] = uint8_t(value >> 8);
  p[3] = uint8_t(value);
}

bool WiderThan16x9(int32_t w, int32_t h) {
  return w > 0 && h > 0 && int64_t(w) * 9 > int64_t(h) * 16;
}

void D3DCreateDeviceUltrawide(PPCContext& ctx, uint8_t* base) {
  int32_t w = 0;
  int32_t h = 0;
  try {
    w = rex::cvar::Query<int32_t>("video_mode_width");
    h = rex::cvar::Query<int32_t>("video_mode_height");
  } catch (...) {
  }
  const uint32_t params = ctx.r4.u32;
  if (params && WiderThan16x9(w, h)) {
    StoreGuestU32(base, params, uint32_t(w));
    StoreGuestU32(base, params + 4, uint32_t(h));
    REXLOG_INFO("Ultrawide backbuffer {}x{}", w, h);
  }
  if (g_d3d_create_device) g_d3d_create_device(ctx, base);
}

}  // namespace

inline void InstallUltrawideBackbuffer(rex::runtime::FunctionDispatcher* dispatcher) {
  if (!dispatcher) return;
  g_d3d_create_device = dispatcher->GetFunction(0x83074148);
  if (!g_d3d_create_device) {
    REXLOG_WARN("D3D CreateDevice 0x83074148 not registered; ultrawide patch skipped");
    return;
  }
  dispatcher->SetFunction(0x83074148, &D3DCreateDeviceUltrawide);
}

}  // namespace wet
