#pragma once

// Strong-symbol overrides for WET's D3D CreateDevice path live in
// wet_ultrawide.cpp. Generated `bl` calls the C++ name, not the dispatcher.
namespace wet {
void RetainUltrawideHooks();
}
