﻿using ControllerCommon.Utils;
using System;
using System.Numerics;

namespace ControllerCommon
{
    public class SensorFusion
    {
        // Gravity Simple
        public Vector3 GravityVectorSimple;

        // Gravity Fancy
        private float Shakiness;
        private Vector3 SmoothAccel;
        private Vector3 GravityVectorFancy;

        // Player Space
        public double CameraYawDelta;
        public double CameraPitchDelta;
        private double AdditionalFactor = 60.0; // Bring more on par with gyro only

        // Device Angle
        public Vector2 DeviceAngle;

        public SensorFusion()
        {
        }

        public void UpdateReport(double TotalMilliseconds, double DeltaSeconds, Vector3 AngularVelocity, Vector3 Acceleration)
        {
            // Check for empty inputs, prevent NaN computes
            Vector3 EmptyVector = new(0f, 0f, 0f);

            if (AngularVelocity.Equals(EmptyVector) || Acceleration.Equals(EmptyVector))
                return;

            // Perform calculations 
            // Todo, kickstart gravity vector with = acceleration when calculation is either
            // run for the first time or is selcted to be run based on user profile?

            CalculateGravitySimple(TotalMilliseconds, DeltaSeconds, AngularVelocity, Acceleration);
            //CalculateGravityFancy(TotalMilliseconds, DeltaSeconds, AngularVelocity, Acceleration);

            DeviceAngles(TotalMilliseconds, GravityVectorSimple);
            PlayerSpace(TotalMilliseconds, DeltaSeconds, AngularVelocity, GravityVectorSimple);
        }

        public void CalculateGravitySimple(double TotalMilliseconds, double DeltaMilliseconds, Vector3 AngularVelocity, Vector3 Acceleration)
        {
            // Gravity determination using sensor fusion, "Something Simple" example from:
            // http://gyrowiki.jibbsmart.com/blog:finding-gravity-with-sensor-fusion

            // Convert to radian as per library spec
            Vector3 AngularVelocityRad = new Vector3(InputUtils.deg2rad(AngularVelocity.X), InputUtils.deg2rad(AngularVelocity.Y), InputUtils.deg2rad(AngularVelocity.Z));

            // Normalize before creating quat from axis angle as per library spec
            AngularVelocityRad = Vector3.Normalize(AngularVelocityRad);

            // Convert gyro input to reverse rotation  
            Quaternion reverseRotation = Quaternion.CreateFromAxisAngle(-AngularVelocityRad, AngularVelocityRad.Length() * (float)DeltaMilliseconds);

            // Rotate gravity vector
            GravityVectorSimple = Vector3.Transform(GravityVectorSimple, reverseRotation);

            // Nudge towards gravity according to current acceleration
            Vector3 newGravity = -Acceleration;
            Vector3 gravityDelta = Vector3.Subtract(newGravity, GravityVectorSimple);

            GravityVectorSimple += Vector3.Multiply(0.02f, Vector3.Normalize(gravityDelta));
        }

        public void CalculateGravityFancy(double TotalMilliseconds, double DeltaTimeSec, Vector3 AngularVelocity, Vector3 Acceleration)
        {
            // TODO Does not work yet!!!

            // Gravity determination using sensor fusion, "Something Fancy" example from:
            // http://gyrowiki.jibbsmart.com/blog:finding-gravity-with-sensor-fusion

            // SETTINGS
            // the time it takes in our acceleration smoothing for 'A' to get halfway to 'B'
            float SmoothingHalfTime = 0.25f;

            // thresholds of trust for accel shakiness. less shakiness = more trust
            float ShakinessMaxThreshold = 0.4f;
            float ShakinessMinThreshold = 0.27f;//0.01f;

            // when we trust the accel a lot (the controller is "still"), how quickly do we correct our gravity vector?
            float CorrectionStillRate = 1f;
            // when we don't trust the accel (the controller is "shaky"), how quickly do we correct our gravity vector?
            float CorrectionShakyRate = 0.1f;

            // if our old gravity vector is close enough to our new one, limit further corrections to this proportion of the rotation speed
            float CorrectionGyroFactor = 0.1f;
            // thresholds for what's considered "close enough"
            float CorrectionGyroMinThreshold = 0.05f;
            float CorrectionGyroMaxThreshold = 0.25f;

            // no matter what, always apply a minimum of this much correction to our gravity vector
            float CorrectionMinimumSpeed = 0.01f;

            // Question
            // Isn't this always true with the default settings? if (ShakinessMaxThreshold > ShakinessMinThreshold)

            // Convert to radian as per library spec
            Vector3 AngularVelocityRad = new Vector3(InputUtils.deg2rad(AngularVelocity.X), InputUtils.deg2rad(AngularVelocity.Y), InputUtils.deg2rad(AngularVelocity.Z));
            // Normalize before creating quat from axis angle as per library spec
            AngularVelocityRad = Vector3.Normalize(AngularVelocityRad);

            // convert gyro input to reverse rotation  
            Quaternion reverseRotation = Quaternion.CreateFromAxisAngle(-AngularVelocityRad, AngularVelocityRad.Length() * (float)DeltaTimeSec);

            // rotate gravity vector
            GravityVectorFancy = Vector3.Transform(GravityVectorFancy, reverseRotation);

            // Correction factor variables
            SmoothAccel = Vector3.Transform(SmoothAccel, reverseRotation);

            // Note to self, SmoothAccel seems OK.
            float smoothInterpolator = (float)Math.Pow(2, (-(float)DeltaTimeSec / SmoothingHalfTime));
            // Note to self, SmoothInterpolator seems OK, still no sure about the Pow from C++ to C#, also, is it suppose to be a negative value?

            Shakiness *= smoothInterpolator;
            Shakiness = Math.Max(Shakiness, Vector3.Subtract(Acceleration, SmoothAccel).Length()); // Does this apply vector subtract and length correctly?
            SmoothAccel = Vector3.Lerp(Acceleration, SmoothAccel, smoothInterpolator); // smoothInterpolator is a negative value, correct?

            Vector3 gravityDelta = Vector3.Subtract(-Acceleration, GravityVectorFancy);
            Vector3 gravityDirection = Vector3.Normalize(gravityDelta);
            float correctionRate;

            // Shakiness correction rate impact
            if (ShakinessMaxThreshold > ShakinessMinThreshold)
            {
                float stillOrShaky = Math.Clamp((Shakiness - ShakinessMinThreshold) / (ShakinessMaxThreshold - ShakinessMaxThreshold), 0, 1);
                correctionRate = CorrectionStillRate + (CorrectionShakyRate - CorrectionStillRate) * stillOrShaky;
                // 1 + (0.1 - 1) * 1 = 0.1
                // Note, found still or shaky to be a constant 1, correction rate to be a constant 0.1

            }
            else if (Shakiness > ShakinessMaxThreshold)
            {
                correctionRate = CorrectionShakyRate;
            }
            else
            {
                correctionRate = CorrectionStillRate;
            }

            // limit in proportion to rotation rate
            // my input library has the gyro report degrees per second, so convert to radians per second here
            float angleRate = AngularVelocity.Length() * (float)Math.PI / 180;
            float correctionLimit = angleRate * GravityVectorFancy.Length() * CorrectionGyroFactor;

            if (correctionRate > correctionLimit)
            {
                float closeEnoughFactor;

                if (CorrectionGyroMaxThreshold > CorrectionGyroMinThreshold)
                {
                    closeEnoughFactor = Math.Clamp((gravityDelta.Length() - CorrectionGyroMinThreshold) / (CorrectionGyroMaxThreshold - CorrectionGyroMinThreshold), 0, 1);
                }
                else if (gravityDelta.Length() > CorrectionGyroMaxThreshold)
                {
                    closeEnoughFactor = 1;
                }
                else
                {
                    closeEnoughFactor = 0;
                }
                correctionRate += (correctionLimit - correctionRate) * closeEnoughFactor;
            }

            // finally, let's always allow a little bit of correction
            correctionRate = Math.Max(correctionRate, CorrectionMinimumSpeed);

            // apply correction
            Vector3 correction = gravityDirection * (float)(correctionRate * DeltaTimeSec);

            if (correction.LengthSquared() < gravityDelta.LengthSquared())
            {
                GravityVectorFancy += correction;
            }
            else
            {
                GravityVectorFancy += gravityDelta;
            }
        }

        private void PlayerSpace(double TotalMilliseconds, double DeltaSeconds, Vector3 AngularVelocity, Vector3 GravityVector)
        {
            // PlayerSpace
            Vector3 GravityNorm = Vector3.Normalize(GravityVector);

            // Yaw (Use world yaw for yaw direction, local combined yaw for magnitude)
            // Dot product but just yaw and roll
            double worldYaw = AngularVelocity.Y * GravityNorm.Y + AngularVelocity.Z * GravityNorm.Z;

            // Handle NaN
            if (worldYaw == 0f) return;

            double yawRelaxFactor = 1.41f;
            Vector2 AngularVelocityYZ = new(AngularVelocity.Y, AngularVelocity.Z);

            CameraYawDelta = Math.Sign(worldYaw)
                                    * Math.Min(Math.Abs(worldYaw) * yawRelaxFactor, AngularVelocityYZ.Length())
                                    * AdditionalFactor * DeltaSeconds;

            // Pitch (local space)
            CameraPitchDelta = AngularVelocity.X * AdditionalFactor * DeltaSeconds;
        }

        private void DeviceAngles(double TotalMilliseconds, Vector3 GravityVector)
        {

            // Calculate angles around Y and X axis (Theta and Psi) using all 3 directions of accelerometer
            // Based on: https://www.digikey.com/en/articles/using-an-accelerometer-for-inclination-sensing               
            DeviceAngle.X = (float)((Math.Atan(GravityVector.Y / (Math.Sqrt(Math.Pow(GravityVector.X, 2) + Math.Pow(GravityVector.Z, 2))))) * 180 / Math.PI);
            DeviceAngle.Y = (float)((Math.Atan(GravityVector.X / (Math.Sqrt(Math.Pow(GravityVector.Y, 2) + Math.Pow(GravityVector.Z, 2))))) * 180 / Math.PI);
        }
    }
}