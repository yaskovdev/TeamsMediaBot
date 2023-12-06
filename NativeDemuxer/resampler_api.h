#pragma once

#include <cstdint>

#include "resampler.h"

extern "C" __declspec(dllexport) resampler* resampler_create();

extern "C" __declspec(dllexport) void resampler_write_frame(resampler* resampler, const uint8_t* frame, int length, int timestamp);

extern "C" __declspec(dllexport) uint8_t* resampler_read_frame(resampler* resampler, frame_metadata* metadata);

extern "C" __declspec(dllexport) void resampler_delete(const resampler* resampler);
