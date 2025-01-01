import serial
import sys
import os
import re

# Check if the COM port argument is provided
if len(sys.argv) != 2:
    print("Usage: python read_serial.py <COM_PORT>")
    sys.exit(1)

# Get the COM port from the command line argument
serial_port = sys.argv[1]
baud_rate = 9600

output_dir = "../reports"
output_file = os.path.join(output_dir, "testResults.log")

os.makedirs(output_dir, exist_ok=True)

try:
    # Open the serial connection
    ser = serial.Serial(serial_port, baud_rate, timeout=1)

    # Flag to indicate when to start writing data to the file
    start_writing = False

    # Open a file to write the serial data
    with open(output_file, "w") as file:
        print(f"Reading data from {serial_port}...")

        while True:
            # Read data from the serial port
            if ser.in_waiting > 0:
                data = ser.readline().decode('utf-8').strip()

                # Check if we received the "Starting tests..." line
                if data == "Starting tests...":
                    start_writing = True
                    continue

                # If "Starting tests..." line has been received, write the data to the file
                if start_writing:
                    print(f"{data}")
                    file.write(data + "\n")

                if data.startswith("Test summary"):
                    match = re.search(r"(\d+) passed", data)
                    if match:
                        passed_tests_count = int(match.group(1))
                        coverage = passed_tests_count/(9+2+1+4+1+2+4+1+6)
                        coverage_percentege = "Coverage: " +  str(coverage*100) + "%\n"
                        file.write(coverage_percentege)
                        print(coverage_percentege)
                    break

except serial.SerialException as e:
    print(f"Error: Could not open {serial_port}. {e}")

sys.exit(0)