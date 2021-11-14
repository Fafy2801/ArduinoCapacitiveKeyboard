#include<CapacitiveSensor.h>

int PIN_OUT = 3;

int pins[4] = {5, 6, 10, 11};
int leds[4] = {4, 7, 12, 13};

int iterations = sizeof(pins) / sizeof(int);

CapacitiveSensor sensors[] = {};

void setup() {
  Serial.begin(9600);

  for(int i = 0; i < iterations; i++)
    pinMode(leds[i], OUTPUT);

  for(int i = 0; i < iterations; i++){
      sensors[i] = CapacitiveSensor(PIN_OUT, pins[i]);
      sensors[i].set_CS_AutocaL_Millis(0xFFFFFFFF);
  }
}

long reading = 0;
byte out = 0;

void loop() {
  out = 0;
  for(int i = 0; i < iterations; i++){
    reading = sensors[i].capacitiveSensor(100);

    if(reading > 25){
      bitSet(out, i);
      digitalWrite(leds[i], HIGH);
    } else
      digitalWrite(leds[i], LOW);
  }

  Serial.write(out);
  delay(10);
}
