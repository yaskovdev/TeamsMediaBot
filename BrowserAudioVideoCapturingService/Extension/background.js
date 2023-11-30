// @ts-nocheck
/* global chrome, MediaRecorder */

let recorder = null

function START_RECORDING({ video, audio, timeSliceMs, audioBitsPerSecond, videoBitsPerSecond, bitsPerSecond, mimeType, videoConstraints }) {
    chrome.tabCapture.capture(
        {
            audio,
            video,
            videoConstraints
        },
        (stream) => {
            if (!stream) return

            recorder = new MediaRecorder(stream, {
                audioBitsPerSecond,
                videoBitsPerSecond,
                bitsPerSecond,
                mimeType
            })
            // TODO: recorder onerror

            recorder.ondataavailable = async function (event) {
                if (event.data.size > 0) {
                    const buffer = await event.data.arrayBuffer()
                    const data = arrayBufferToString(buffer)

                    if (window.sendData) {
                        window.sendData(data)
                    }
                }
            }
            recorder.onerror = () => recorder.stop()

            recorder.onstop = function () {
                try {
                    const tracks = stream.getTracks()

                    tracks.forEach(function (track) {
                        track.stop()
                    })
                } catch (error) {}
            }
            stream.oninactive = () => {
                try {
                    recorder.stop()
                } catch (error) {}
            }

            recorder.start(timeSliceMs)
        }
    )
}

function STOP_RECORDING() {
    if (recorder) recorder.stop()
}

/**
 * Convert an ArrayBuffer to a UTF-8 string.
 */
function arrayBufferToString(buffer) {
    const bufView = new Uint8Array(buffer)
    const length = bufView.length
    let result = ''
    let addition = Math.pow(2, 8) - 1

    for (let i = 0; i < length; i += addition) {
        if (i + addition > length) {
            addition = length - i
        }
        result += String.fromCharCode.apply(null, bufView.subarray(i, i + addition))
    }
    return result
}
