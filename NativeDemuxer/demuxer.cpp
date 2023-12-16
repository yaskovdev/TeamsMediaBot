#include "demuxer.h"

#include <iostream>

extern "C" {
#include "libavutil/imgutils.h"
#include "libavformat/avformat.h"
#include "libavcodec/avcodec.h"
}

demuxer::demuxer(const callback callback): initialized_(false), callback_(callback), fmt_ctx_(nullptr), frame_(nullptr), pkt_(nullptr), width_(0), height_(0),
    pix_fmt_(AV_PIX_FMT_NONE), video_dst_bufsize_(0), video_dst_data_{}, video_dst_linesize_{}, audio_stream_idx_(-1), video_stream_idx_(-1),
    audio_dec_ctx_(nullptr), video_dec_ctx_(nullptr), sws_context_(nullptr), decoder_needs_packet_(true), current_stream_index_(-1)
{
    std::cout << "Demuxer instantiated" << "\n";
}

void demuxer::initialize()
{
    fmt_ctx_ = avformat_alloc_context();
    std::cout << "Allocated format context " << fmt_ctx_ << "\n";

    constexpr size_t io_ctx_buffer_size = 4096;
    const auto io_ctx_buffer = static_cast<uint8_t*>(av_malloc(io_ctx_buffer_size));
    ASSERT_NOT_NULL(io_ctx_buffer, "Cannot allocate context buffer");

    AVIOContext* io_ctx = avio_alloc_context(io_ctx_buffer, io_ctx_buffer_size, 0, &callback_, &read_packet, nullptr, nullptr);
    ASSERT_NOT_NULL(io_ctx, "Cannot allocate IO context");

    fmt_ctx_->pb = io_ctx;

    ASSERT_NON_NEGATIVE(avformat_open_input(&fmt_ctx_, nullptr, nullptr, nullptr), "Cannot open input");
    ASSERT_NON_NEGATIVE(avformat_find_stream_info(fmt_ctx_, nullptr), "Cannot find stream info");

    open_decoder_context(&video_stream_idx_, &video_dec_ctx_, fmt_ctx_, AVMEDIA_TYPE_VIDEO);
    width_ = video_dec_ctx_->width;
    height_ = video_dec_ctx_->height;
    pix_fmt_ = video_dec_ctx_->pix_fmt;
    sws_context_ = sws_getContext(width_, height_, pix_fmt_, width_, height_, AV_PIX_FMT_NV12, SWS_BILINEAR, nullptr, nullptr, nullptr);
    ASSERT_NOT_NULL(sws_context_, "Cannot allocate scaling context");
    video_dst_bufsize_ = av_image_alloc(video_dst_data_, video_dst_linesize_, width_, height_, AV_PIX_FMT_NV12, 1);
    ASSERT_NON_NEGATIVE(video_dst_bufsize_, "Could not allocate raw video buffer");

    open_decoder_context(&audio_stream_idx_, &audio_dec_ctx_, fmt_ctx_, AVMEDIA_TYPE_AUDIO);

    av_dump_format(fmt_ctx_, 0, nullptr, 0);

    frame_ = av_frame_alloc();
    ASSERT_NOT_NULL(frame_, "Could not allocate frame");

    pkt_ = av_packet_alloc();
    ASSERT_NOT_NULL(pkt_, "Could not allocate packet");
}

uint8_t* demuxer::read_frame(frame_metadata* metadata)
{
    if (!initialized_)
    {
        initialize();
        std::cout << "Initialized demuxer" << "\n";
        initialized_ = true;
    }

    while (true)
    {
        if (decoder_needs_packet_)
        {
            const int read_packet_status = av_read_frame(fmt_ctx_, pkt_);
            if (read_packet_status == AVERROR_EOF)
            {
                return nullptr;
            }
            ASSERT_NON_NEGATIVE(read_packet_status, "Cannot read new packet");
            current_stream_index_ = pkt_->stream_index;
            ASSERT_NON_NEGATIVE(avcodec_send_packet(current_context(), pkt_), "Cannot send packet to decoder");
            decoder_needs_packet_ = false;
        }

        const int decoding_status = avcodec_receive_frame(current_context(), frame_);
        if (decoding_status < 0)
        {
            if (decoding_status == AVERROR_EOF || decoding_status == AVERROR(EAGAIN))
            {
                decoder_needs_packet_ = true;
            }
            else
            {
                ASSERT_NON_NEGATIVE(decoding_status, "Cannot receive decoded frame");
            }
        }
        else if (pkt_->stream_index == video_stream_idx_)
        {
            ASSERT_NON_NEGATIVE(sws_scale(sws_context_, frame_->data, frame_->linesize, 0, frame_->height, video_dst_data_, video_dst_linesize_), "Cannnot scale video frame");
            const int buffer_size = av_image_get_buffer_size(AV_PIX_FMT_NV12, width_, height_, 1);
            auto decoded_data = std::make_unique<uint8_t[]>(buffer_size);
            ASSERT_NON_NEGATIVE(av_image_copy_to_buffer(decoded_data.get(), buffer_size, video_dst_data_, video_dst_linesize_, AV_PIX_FMT_NV12, width_, height_, 1), "Cannnot copy decoded frame to output buffer");
            metadata->type = 0;
            metadata->size = buffer_size;
            metadata->timestamp = frame_->pts;
            return decoded_data.release();
        }
        else
        {
            const size_t buffer_size = frame_->nb_samples * av_get_bytes_per_sample(static_cast<AVSampleFormat>(frame_->format));
            auto decoded_data = std::make_unique<uint8_t[]>(buffer_size);
            memcpy(decoded_data.get(), frame_->extended_data[0], buffer_size);
            metadata->type = 1;
            metadata->size = buffer_size;
            metadata->timestamp = frame_->pts;
            return decoded_data.release();
        }
    }
}

int demuxer::read_packet(void* opaque, uint8_t* dst_buffer, const int dst_buffer_size)
{
    const callback* c = static_cast<callback*>(opaque);
    const int size = (*c)(dst_buffer, dst_buffer_size);
    return size == -1 ? AVERROR_EOF : size;
}

void demuxer::open_decoder_context(int* stream_idx, AVCodecContext** decoder_context, AVFormatContext* fmt_ctx, const AVMediaType type)
{
    *stream_idx = av_find_best_stream(fmt_ctx, type, -1, -1, nullptr, 0);
    ASSERT_NON_NEGATIVE(*stream_idx, "Could not find stream in input file");
    const AVStream* stream = fmt_ctx->streams[*stream_idx];

    const AVCodec* dec = avcodec_find_decoder(stream->codecpar->codec_id);
    ASSERT_NOT_NULL(dec, "Failed to find decoder");

    *decoder_context = avcodec_alloc_context3(dec);
    ASSERT_NOT_NULL(*decoder_context, "Failed to open decoder context");

    ASSERT_NON_NEGATIVE(avcodec_parameters_to_context(*decoder_context, stream->codecpar), "Failed to copy codec parameters to decoder context");
    ASSERT_NON_NEGATIVE(avcodec_open2(*decoder_context, dec, nullptr), "Failed to open decoder");
}

AVCodecContext* demuxer::current_context() const
{
    if (current_stream_index_ == audio_stream_idx_)
    {
        return audio_dec_ctx_;
    }
    if (current_stream_index_ == video_stream_idx_)
    {
        return video_dec_ctx_;
    }
    throw demuxer_exception("Unexpected stream index");
}
