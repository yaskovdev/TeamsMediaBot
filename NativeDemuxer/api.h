#pragma once

#include "demuxer.h"
#include "common.h"

extern "C" __declspec(dllexport) demuxer* create_demuxer(callback callback);

extern "C" __declspec(dllexport) int read_frame(demuxer* demuxer, uint8_t* decoded_data, frame_metadata* metadata);

extern "C" __declspec(dllexport) void delete_demuxer(const demuxer* demuxer);
