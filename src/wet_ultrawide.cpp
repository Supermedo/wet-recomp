#include "wet_ultrawide.h"

#include <rex/cvar.h>
#include <rex/logging/macros.h>
#include <rex/ppc.h>

#include <cstdint>

// Generated functions are weak aliases of __imp__sub_*. Direct `bl` in recomp
// calls the C++ symbol, so FunctionDispatcher::SetFunction never runs. A
// strong sub_* here overrides the alias; __imp__sub_* is the original body.

extern "C" void __imp__sub_830668C8(PPCContext& __restrict ctx, uint8_t* base);
extern "C" void __imp__sub_83074148(PPCContext& __restrict ctx, uint8_t* base);
extern "C" void __imp__sub_83054940(PPCContext& __restrict ctx, uint8_t* base);
extern "C" void __imp__sub_83054A60(PPCContext& __restrict ctx, uint8_t* base);
extern "C" void __imp__sub_82CAB830(PPCContext& __restrict ctx, uint8_t* base);

namespace {

void StoreGuestU32(uint8_t* base, uint32_t ea, uint32_t value) {
  uint8_t* p = base + ea;
  p[0] = uint8_t(value >> 24);
  p[1] = uint8_t(value >> 16);
  p[2] = uint8_t(value >> 8);
  p[3] = uint8_t(value);
}

uint32_t LoadGuestU32(uint8_t* base, uint32_t ea) {
  const uint8_t* p = base + ea;
  return (uint32_t(p[0]) << 24) | (uint32_t(p[1]) << 16) | (uint32_t(p[2]) << 8) |
         uint32_t(p[3]);
}

bool WiderThan16x9(int32_t w, int32_t h) {
  return w > 0 && h > 0 && int64_t(w) * 9 > int64_t(h) * 16;
}

bool VideoMode(int32_t& w, int32_t& h) {
  w = 0;
  h = 0;
  try {
    w = rex::cvar::Query<int32_t>("video_mode_width");
    h = rex::cvar::Query<int32_t>("video_mode_height");
  } catch (...) {
    return false;
  }
  return WiderThan16x9(w, h);
}

// WET only ever creates 16:9 full-frame surfaces (720p / 1080p). Replace those
// with the configured 21:9 size so the scene RT, backbuffer, and FOV match.
bool IsFullFrame16x9(uint32_t w, uint32_t h) {
  if (w == 0 || h == 0) return false;
  if ((w == 1280 && h == 720) || (w == 1920 && h == 1080) || (w == 960 && h == 540)) {
    return true;
  }
  return w >= 640 && int64_t(w) * 9 == int64_t(h) * 16;
}

void PatchSizePair(uint32_t ea, uint8_t* base, const char* tag) {
  int32_t w = 0;
  int32_t h = 0;
  if (!ea || !VideoMode(w, h)) return;
  const uint32_t pw = LoadGuestU32(base, ea);
  const uint32_t ph = LoadGuestU32(base, ea + 4);
  if (!IsFullFrame16x9(pw, ph)) return;
  StoreGuestU32(base, ea, uint32_t(w));
  StoreGuestU32(base, ea + 4, uint32_t(h));
  REXLOG_INFO("Ultrawide {} {}x{} (was {}x{})", tag, w, h, pw, ph);
}

void PatchPresentParams(uint32_t params, uint8_t* base) {
  PatchSizePair(params, base, "present params");
}

void PatchViewConfig(uint32_t params, uint8_t* base) {
  // 82CAD670 stores 1280x720 at +8/+12 of the r4 block.
  PatchSizePair(params, base, "view size");
  PatchSizePair(params + 8, base, "view size");
}

void ForceFullFrameSize(PPCContext& ctx) {
  int32_t w = 0;
  int32_t h = 0;
  if (!VideoMode(w, h)) return;
  if (!IsFullFrame16x9(ctx.r3.u32, ctx.r4.u32)) return;
  REXLOG_INFO("Ultrawide surface {}x{} (was {}x{})", w, h, ctx.r3.u32, ctx.r4.u32);
  ctx.r3.u64 = uint32_t(w);
  ctx.r4.u64 = uint32_t(h);
}

}  // namespace

extern "C" void sub_830668C8(PPCContext& __restrict ctx, uint8_t* base) {
  PatchPresentParams(ctx.r7.u32, base);
  __imp__sub_830668C8(ctx, base);
}

extern "C" void sub_83074148(PPCContext& __restrict ctx, uint8_t* base) {
  PatchPresentParams(ctx.r4.u32, base);
  __imp__sub_83074148(ctx, base);
}

extern "C" void sub_83054940(PPCContext& __restrict ctx, uint8_t* base) {
  ForceFullFrameSize(ctx);
  __imp__sub_83054940(ctx, base);
}

extern "C" void sub_83054A60(PPCContext& __restrict ctx, uint8_t* base) {
  ForceFullFrameSize(ctx);
  __imp__sub_83054A60(ctx, base);
}

extern "C" void sub_82CAB830(PPCContext& __restrict ctx, uint8_t* base) {
  PatchViewConfig(ctx.r4.u32, base);
  __imp__sub_82CAB830(ctx, base);
}

void wet::RetainUltrawideHooks() {}
