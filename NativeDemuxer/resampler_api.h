#pragma once

#include <cstdint>

#include "resampler.h"

extern "C" __declspec(dllexport) resampler* resampler_create();

extern "C" __declspec(dllexport) uint8_t* resampler_resample_frame(resampler* resampler, const uint8_t* src_frame, int src_length, frame_metadata* dst_metadata);

extern "C" __declspec(dllexport) void resampler_frame_buffer_delete(const uint8_t* buffer);

extern "C" __declspec(dllexport) void resampler_delete(const resampler* resampler);
