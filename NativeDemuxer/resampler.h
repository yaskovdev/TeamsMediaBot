#pragma once
#include "common.h"

extern "C" {
#include <libswresample/swresample.h>
}

class resampler
{
    SwrContext* resample_context_;
    int src_rate_;
    AVSampleFormat src_sample_format_;
    int dst_rate_;
    AVSampleFormat dst_sample_format_;
    uint8_t** dst_data_;
    int dst_bufsize_;
    int dst_linesize_;
    int dst_nb_samples_;
    int max_dst_nb_samples_;
    AVChannelLayout src_ch_layout_;
    AVChannelLayout dst_ch_layout_;

public:
    resampler();

    uint8_t* resample_frame(const uint8_t* src_frame, int src_length, frame_metadata* dst_metadata);
};
