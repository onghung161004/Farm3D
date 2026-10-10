import wave
import struct
import math
import random
import os

sample_rate = 44100

def write_wav(filename, samples):
    with wave.open(filename, 'w') as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(sample_rate)
        for s in samples:
            # clip and pack
            s = max(-1.0, min(1.0, s))
            val = int(s * 32767.0)
            wav_file.writeframesraw(struct.pack('<h', val))

def generate_noise(duration, decay_factor):
    samples = []
    num_samples = int(sample_rate * duration)
    for i in range(num_samples):
        env = math.exp(-i / (sample_rate * decay_factor))
        s = random.uniform(-1.0, 1.0) * env
        samples.append(s)
    return samples

def generate_tone(freq, duration, decay_factor):
    samples = []
    num_samples = int(sample_rate * duration)
    for i in range(num_samples):
        env = math.exp(-i / (sample_rate * decay_factor))
        s = math.sin(2 * math.pi * freq * i / sample_rate) * env
        samples.append(s)
    return samples

def generate_harvest():
    # ascending notes
    samples = []
    dur = 0.15
    for freq in [440, 554, 659]: # A, C#, E
        num_samples = int(sample_rate * dur)
        for i in range(num_samples):
            env = math.exp(-i / (sample_rate * 0.1))
            s = math.sin(2 * math.pi * freq * i / sample_rate) * env
            samples.append(s * 0.5)
    return samples

os.makedirs('Assets/FarmRestoration/Audio', exist_ok=True)

# Till: low noise (sounds like digging)
write_wav('Assets/FarmRestoration/Audio/tillSound.wav', generate_noise(0.3, 0.05))

# Plant: short pop (sounds like dropping a seed)
write_wav('Assets/FarmRestoration/Audio/plantSound.wav', generate_tone(600, 0.1, 0.02))

# Water: splashy noise
samples_water = generate_noise(0.1, 0.02) + generate_noise(0.1, 0.02) + generate_noise(0.2, 0.05)
write_wav('Assets/FarmRestoration/Audio/waterSound.wav', samples_water)

# Harvest: pleasant ascending chime
write_wav('Assets/FarmRestoration/Audio/harvestSound.wav', generate_harvest())

print("Successfully generated 4 placeholder sound files in Assets/FarmRestoration/Audio")
