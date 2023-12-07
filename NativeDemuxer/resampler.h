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
    int src_timestamp_;
    int dst_rate_;
    AVSampleFormat dst_sample_format_;
    uint8_t** dst_data_;
    int dst_bufsize_;
    int dst_linesize_;
    int dst_nb_samples_;
    int max_dst_nb_samples_;
    AVChannelLayout src_ch_layout_;
    AVChannelLayout dst_ch_layout_;
    bool frame_consumed_;

public:
    resampler();

    void write_frame(const uint8_t* frame, int length, int timestamp);

    uint8_t* read_frame(frame_metadata* metadata);
};
