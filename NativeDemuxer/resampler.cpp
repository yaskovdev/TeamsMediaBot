#include "resampler.h"

#include <iostream>
#include <memory>

#include "common.h"

extern "C" {
#include "libavutil/samplefmt.h"
}

resampler::resampler() : src_rate_(48000), src_sample_format_(AV_SAMPLE_FMT_FLT), dst_rate_(16000), dst_sample_format_(AV_SAMPLE_FMT_S16), dst_nb_samples_(1),
    src_ch_layout_(AV_CHANNEL_LAYOUT_MONO), dst_ch_layout_(AV_CHANNEL_LAYOUT_MONO), frame_consumed_(false)
{
    constexpr int src_nb_samples = 441;
    const int dst_nb_samples = av_rescale_rnd(src_nb_samples, dst_rate_, src_rate_, AV_ROUND_UP);
    max_dst_nb_samples_ = dst_nb_samples;
    ASSERT_NON_NEGATIVE(av_samples_alloc_array_and_samples(&dst_data_, &dst_linesize_, dst_ch_layout_.nb_channels, dst_nb_samples_, dst_sample_format_, 0), "Cannot allocate output buffer");
    ASSERT_NON_NEGATIVE(swr_alloc_set_opts2(&resample_context_, &dst_ch_layout_, dst_sample_format_, dst_rate_, &dst_ch_layout_, src_sample_format_, src_rate_, 0, nullptr), "Cannot create SWR context");
    ASSERT_NON_NEGATIVE(swr_init(resample_context_), "Cannot initialize SWR context");
}

void resampler::write_frame(const uint8_t* frame, const int length)
{
    const int src_nb_samples = length / av_get_bytes_per_sample(src_sample_format_);
    dst_nb_samples_ = av_rescale_rnd(swr_get_delay(resample_context_, src_rate_) + src_nb_samples, dst_rate_, src_rate_, AV_ROUND_UP);
    if (dst_nb_samples_ > max_dst_nb_samples_)
    {
        av_freep(&dst_data_[0]);
        ASSERT_NON_NEGATIVE(av_samples_alloc(dst_data_, &dst_linesize_, dst_ch_layout_.nb_channels, dst_nb_samples_, dst_sample_format_, 1), "Cannot allocate samples");
        max_dst_nb_samples_ = dst_nb_samples_;
    }
    const int samples_per_channel = swr_convert(resample_context_, dst_data_, dst_nb_samples_, &frame, src_nb_samples);
    ASSERT_NON_NEGATIVE(samples_per_channel, "Cannot convert audio");
    dst_bufsize_ = av_samples_get_buffer_size(&dst_linesize_, dst_ch_layout_.nb_channels, samples_per_channel, dst_sample_format_, 1);
    ASSERT_NON_NEGATIVE(dst_bufsize_, "Cannot calculate dst buffer size");
    frame_consumed_ = false;
}

uint8_t* resampler::read_frame(frame_metadata* metadata)
{
    if (frame_consumed_)
    {
        return nullptr;
    }
    frame_consumed_ = true;
    auto decoded_data = std::make_unique<uint8_t[]>(dst_bufsize_);
    memcpy(decoded_data.get(), dst_data_[0], dst_bufsize_);
    metadata->type = 1;
    metadata->size = dst_bufsize_;
    return decoded_data.release();
}
