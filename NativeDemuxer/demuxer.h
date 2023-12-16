#pragma once
#include <cstdint>

#include "common.h"

extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libswscale/swscale.h>
}

struct buffer_data
{
    uint8_t* ptr;
    int offset;
    size_t size;
    callback callback;
};

class demuxer
{
    bool initialized_;
    callback callback_;
    AVFormatContext* fmt_ctx_;
    AVFrame* frame_;
    AVPacket* pkt_;
    int width_;
    int height_;
    AVPixelFormat pix_fmt_;
    int video_dst_bufsize_;
    uint8_t* video_dst_data_[4];
    int video_dst_linesize_[4];
    int audio_stream_idx_;
    int video_stream_idx_;
    AVCodecContext* audio_dec_ctx_;
    AVCodecContext* video_dec_ctx_;
    SwsContext* sws_context_;
    bool decoder_needs_packet_;
    int current_stream_index_;

    void initialize();

    AVCodecContext* current_context() const;

    static int read_packet(void* opaque, uint8_t* dst_buffer, int dst_buffer_size);

    static void open_decoder_context(int* stream_idx, AVCodecContext** decoder_context, AVFormatContext* fmt_ctx, AVMediaType type);

public:
    demuxer(callback callback);

    /**
     * \brief May return nullptr if end of stream reached.
     */
    uint8_t* read_frame(frame_metadata* metadata);
};
