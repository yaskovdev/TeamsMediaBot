#include "demuxer.h"

#include <iostream>

extern "C" {
#include "libavutil/file.h"
#include "libavutil/imgutils.h"
#include "libavformat/avformat.h"
#include "libavcodec/avcodec.h"
}

#define ASSERT_NON_NEGATIVE(res, message) if ((res) < 0) throw std::runtime_error(message)

#define ASSERT_NOT_NULL(res, message) if ((res) == nullptr) throw std::runtime_error(message)

demuxer::demuxer(const callback callback): initialized_(false), callback_(callback), fmt_ctx_(nullptr), frame_(nullptr), pkt_(nullptr), width_(0), height_(0),
    pix_fmt_(AV_PIX_FMT_NONE), video_dst_bufsize_(0), video_dst_data_{}, video_dst_linesize_{}, audio_stream_idx_(-1), video_stream_idx_(-1),
    audio_dec_ctx_(nullptr), video_dec_ctx_(nullptr), sws_context_(nullptr), decoder_needs_packet_(true), current_stream_index_(-1)
{
    std::cout << "Demuxer created" << "\n";
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

    ASSERT_NON_NEGATIVE(open_codec_context(&video_stream_idx_, &video_dec_ctx_, fmt_ctx_, AVMEDIA_TYPE_VIDEO), "Cannot open video context");
    width_ = video_dec_ctx_->width;
    height_ = video_dec_ctx_->height;
    pix_fmt_ = video_dec_ctx_->pix_fmt;
    sws_context_ = sws_getContext(width_, height_, pix_fmt_, width_, height_, AV_PIX_FMT_NV12, SWS_BILINEAR, nullptr, nullptr, nullptr);
    video_dst_bufsize_ = av_image_alloc(video_dst_data_, video_dst_linesize_, width_, height_, AV_PIX_FMT_NV12, 1);
    ASSERT_NON_NEGATIVE(video_dst_bufsize_, "Could not allocate raw video buffer");

    ASSERT_NON_NEGATIVE(open_codec_context(&audio_stream_idx_, &audio_dec_ctx_, fmt_ctx_, AVMEDIA_TYPE_AUDIO), "Cannot open audio context");

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
        try
        {
            initialize();
            std::cout << "Initialized demuxer" << "\n";
            initialized_ = true;
        }
        catch (std::runtime_error& e)
        {
            std::cout << "Cannot initialize demuxer, not enough data in the buffer, send more data. The error is " << e.what() << "\n";
            return nullptr;
        }
    }

    while (true)
    {
        if (decoder_needs_packet_)
        {
            if (av_read_frame(fmt_ctx_, pkt_) < 0)
            {
                return nullptr;
            }
            current_stream_index_ = pkt_->stream_index;
            if (avcodec_send_packet(current_context(), pkt_) < 0)
            {
                exit(1); // TODO: throw exception instead
            }
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
                exit(1);
            }
        }
        else if (pkt_->stream_index == video_stream_idx_)
        {
            sws_scale(sws_context_, frame_->data, frame_->linesize, 0, frame_->height, video_dst_data_, video_dst_linesize_);
            const int buffer_size = av_image_get_buffer_size(AV_PIX_FMT_NV12, width_, height_, 1);
            auto decoded_data = std::make_unique<uint8_t[]>(buffer_size);
            if (av_image_copy_to_buffer(decoded_data.get(), buffer_size, video_dst_data_, video_dst_linesize_, AV_PIX_FMT_NV12, width_, height_, 1) < 0)
            {
                exit(1);
            }
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

int demuxer::open_codec_context(int* stream_idx, AVCodecContext** dec_ctx, AVFormatContext* fmt_ctx, AVMediaType type)
{
    int ret, stream_index;
    AVStream* st;

    ret = av_find_best_stream(fmt_ctx, type, -1, -1, nullptr, 0);
    if (ret < 0)
    {
        fprintf(stderr, "Could not find %s stream in input file\n", av_get_media_type_string(type));
        return ret;
    }
    else
    {
        stream_index = ret;
        st = fmt_ctx->streams[stream_index];

        /* find decoder for the stream */
        const AVCodec* dec = avcodec_find_decoder(st->codecpar->codec_id);
        if (!dec)
        {
            fprintf(stderr, "Failed to find %s codec\n",
                av_get_media_type_string(type));
            return AVERROR(EINVAL);
        }

        /* Allocate a codec context for the decoder */
        *dec_ctx = avcodec_alloc_context3(dec);
        if (!*dec_ctx)
        {
            fprintf(stderr, "Failed to allocate the %s codec context\n",
                av_get_media_type_string(type));
            return AVERROR(ENOMEM);
        }

        /* Copy codec parameters from input stream to output codec context */
        if ((ret = avcodec_parameters_to_context(*dec_ctx, st->codecpar)) < 0)
        {
            fprintf(stderr, "Failed to copy %s codec parameters to decoder context\n",
                av_get_media_type_string(type));
            return ret;
        }

        /* Init the decoders */
        if ((ret = avcodec_open2(*dec_ctx, dec, nullptr)) < 0)
        {
            fprintf(stderr, "Failed to open %s codec\n",
                av_get_media_type_string(type));
            return ret;
        }
        *stream_idx = stream_index;
    }

    return 0;
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
    // TODO: handle this case properly (should skip such packet)
    exit(1);
}
