#pragma once
#include <exception>

typedef int (__stdcall *callback)(uint8_t*, int);

struct frame_metadata
{
    int type;
    size_t size;
    int64_t timestamp;
};

class demuxer_exception final : public std::exception
{
    const char* message_;

public:
    explicit demuxer_exception(const char* message) : message_(message)
    {
    }

    const char* what() const override
    {
        return message_;
    }
};

#define ASSERT_NON_NEGATIVE(res, message) if ((res) < 0) throw demuxer_exception(message)

#define ASSERT_NOT_NULL(res, message) if ((res) == nullptr) throw demuxer_exception(message)
