'''  
Copyright (c) 2019 Diogo Gomes.
Licensed under the MIT license. See LICENSE file in the project root for full license information.

Copyright (c) 2017 Intel Corporation.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
'''

import cv2
import numpy as np
import argparse

def avg_circles(circles, b):
    avg_x=0
    avg_y=0
    avg_r=0
    for i in range(b):
        #optional - average for multiple circles (can happen when a gauge is at a slight angle)
        avg_x = avg_x + circles[0][i][0]
        avg_y = avg_y + circles[0][i][1]
        avg_r = avg_r + circles[0][i][2]
    avg_x = int(avg_x/(b))
    avg_y = int(avg_y/(b))
    avg_r = int(avg_r/(b))
    return avg_x, avg_y, avg_r

def dist_2_pts(x1, y1, x2, y2):
    return np.sqrt((x2 - x1)**2 + (y2 - y1)**2)

def find_gauge(img, gauge_pixels_radius):
    height, width = img.shape[:2]
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)  #convert to gray
    #gray = cv2.GaussianBlur(gray, (5, 5), 0)
    # gray = cv2.medianBlur(gray, 5)

    #for testing, output gray image
    #cv2.imwrite('gauge-%s-bw.%s' %(gauge_number, file_type),gray)

    #detect circles
    #restricting the search from 35-48% of the possible radii gives fairly good results across different samples.  Remember that
    #these are pixel values which correspond to the possible radii search range.
    circles = cv2.HoughCircles(gray, cv2.HOUGH_GRADIENT, 1, 20, np.array([]), param1=50,param2=30,minRadius=int(gauge_pixels_radius*0.9),maxRadius=int(gauge_pixels_radius*1.1))
    # average found circles, found it to be more accurate than trying to tune HoughCircles parameters to get just the right one
    a, b, c = circles.shape
    x,y,r = avg_circles(circles, b)
    return x, y, r

def calibrate_gauge(img, gauge_pixels_radius):
    x, y, r = find_gauge(img, gauge_pixels_radius)

    #draw center and circle
    cv2.circle(img, (x, y), r, (0, 0, 255), 3, cv2.LINE_AA)  # draw circle
    cv2.circle(img, (x, y), 2, (0, 255, 0), 3, cv2.LINE_AA)  # draw center of circle

    #for testing, output circles on image
    #cv2.imwrite('gauge-%s-circles.%s' % (gauge_number, file_type), img)


    #for calibration, plot lines from center going out at every 10 degrees and add marker
    #for i from 0 to 36 (every 10 deg)

    '''
    goes through the motion of a circle and sets x and y values based on the set separation spacing.  Also adds text to each
    line.  These lines and text labels serve as the reference point for the user to enter
    NOTE: by default this approach sets 0/360 to be the +x axis (if the image has a cartesian grid in the middle), the addition
    (i+9) in the text offset rotates the labels by 90 degrees so 0/360 is at the bottom (-y in cartesian).  So this assumes the
    gauge is aligned in the image, but it can be adjusted by changing the value of 9 to something else.
    '''
    separation = 10.0 #in degrees
    interval = int(360 / separation)
    p1 = np.zeros((interval,2))  #set empty arrays
    p2 = np.zeros((interval,2))
    p_text = np.zeros((interval,2))
    for i in range(0,interval):
        for j in range(0,2):
            if (j%2==0):
                p1[i][j] = x + 0.9 * r * np.cos(separation * i * 3.14 / 180) #point for lines
            else:
                p1[i][j] = y + 0.9 * r * np.sin(separation * i * 3.14 / 180)
    text_offset_x = 10
    text_offset_y = 5
    for i in range(0, interval):
        for j in range(0, 2):
            if (j % 2 == 0):
                p2[i][j] = x + r * np.cos(separation * i * 3.14 / 180)
                p_text[i][j] = x - text_offset_x + 1.2 * r * np.cos((separation) * (i+9) * 3.14 / 180) #point for text labels, i+9 rotates the labels by 90 degrees
            else:
                p2[i][j] = y + r * np.sin(separation * i * 3.14 / 180)
                p_text[i][j] = y + text_offset_y + 1.2* r * np.sin((separation) * (i+9) * 3.14 / 180)  # point for text labels, i+9 rotates the labels by 90 degrees

    #add the lines and labels to the image
    for i in range(0,interval):
        cv2.line(img, (int(p1[i][0]), int(p1[i][1])), (int(p2[i][0]), int(p2[i][1])),(0, 255, 0), 2)
        cv2.putText(img, '%s' %(int(i*separation)), (int(p_text[i][0]), int(p_text[i][1])), cv2.FONT_HERSHEY_SIMPLEX, 0.3,(0,0,0),1,cv2.LINE_AA)

    cv2.imshow('Calibration', img)
    cv2.waitKey(0)
    cv2.destroyAllWindows()

    return x, y, r

def get_current_value(img, min_angle, max_angle, min_value, max_value, gauge_pixels_radius, debug):
    x, y, r = find_gauge(img, gauge_pixels_radius)

    gray2 = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    # Set threshold and maxValue
    thresh = 90
    maxValue = 255

    # apply thresholding which helps for finding lines
    th, dst2 = cv2.threshold(gray2, thresh, maxValue, cv2.THRESH_BINARY_INV);

    # found Hough Lines generally performs better without Canny / blurring, though there were a couple exceptions where it would only work with Canny / blurring
    # dst2 = cv2.medianBlur(dst2, 5)
    # dst2 = cv2.Canny(dst2, 50, 150)
    # dst2 = cv2.GaussianBlur(dst2, (10, 10), 0)

    contours = cv2.findContours(dst2, mode=cv2.RETR_LIST, method=cv2.CHAIN_APPROX_SIMPLE)[-2]

    # get contours that are closest to the circle of the gauge
    closest_contour = None
    min_dist = 10000
    for contour in contours:
        (x1, y1), (w, h), angle = cv2.minAreaRect(contour)
        dist = dist_2_pts(x, y, x1, y1)
        if dist < min_dist:
            closest_contour = contour
            min_dist = dist

    # ensure that the contour is within the circle of the gauge
    if min_dist > r:
        raise Exception("No valid gauge found")
    
    # find the furtherst point from the center to be what is used to determine the angle
    # get all points in the contour
    all_points = np.vstack(closest_contour)

    # get the farthest point from the center
    x1, y1 = all_points[np.argmax(dist_2_pts(x, y, all_points[:, 0], all_points[:, 1]))]

    if debug:     
        cv2.drawContours(img, [closest_contour], -1, (255,0,0), 1)
        # show circle and center
        # cv2.circle(img, (x, y), r, (255, 0, 0), 2)
        # show X (marks the spot) at the farthest point found
        cv2.circle(img, (x1, y1), 5, (0, 0, 255), 2)
        cv2.imshow('Show line', img)
        cv2.waitKey(0)
        cv2.destroyAllWindows()

    

    #find the farthest point from the center to be what is used to determine the angle
    x_angle = x1 - x
    y_angle = y - y1
    # take the arc tan of y/x to find the angle
    res = np.arctan(np.divide(float(y_angle), float(x_angle)))

    #these were determined by trial and error
    res = np.rad2deg(res)
    if x_angle > 0 and y_angle > 0:  #in quadrant I
        final_angle = 270 - res
    if x_angle < 0 and y_angle > 0:  #in quadrant II
        final_angle = 90 - res
    if x_angle < 0 and y_angle < 0:  #in quadrant III
        final_angle = 90 - res
    if x_angle > 0 and y_angle < 0:  #in quadrant IV
        final_angle = 270 - res

    if final_angle > 180:
        final_angle -= 180

    old_min = float(min_angle)
    old_max = float(max_angle)

    new_min = float(min_value)
    new_max = float(max_value)

    old_value = final_angle

    old_range = (old_max - old_min)
    new_range = (new_max - new_min)
    new_value = (((old_value - old_min) * new_range) / old_range) + new_min

    return final_angle, new_value

def main():
    parser = argparse.ArgumentParser()
    required = parser.add_argument_group('required arguments')
    parser.add_argument('filename', metavar='filename', type=argparse.FileType('r'),
                    help='file containing image of gauge')
    parser.add_argument("--calibrate", help="Generate calibration image", action='store_true')
    parser.add_argument("--debug", help="Show debug info", type=bool, default=False)
    parser.add_argument("--gauge_radius", help="Aproximate radius of the gauge in pixels", type=int, required=True)

    opts, rem_args = parser.parse_known_args()

    if not opts.calibrate:
        required.add_argument("--min_angle", help="Min angle (lowest possible angle of dial) - in degrees", type=int, required=True)
        required.add_argument("--max_angle", help="Max angle (highest possible angle) - in degrees", type=int, required=True)
        required.add_argument("--min_value", help="Min value", type=float, required=True)
        required.add_argument("--max_value", help="Max value", type=float, required=True)

    args = parser.parse_args()

    img = cv2.imread(args.filename.name)

    if args.calibrate:
        calibrate_gauge(img, args.gauge_radius)
        return

    ang, val = get_current_value(img, args.min_angle, args.max_angle, args.min_value, args.max_value, args.gauge_radius, args.debug)
    # output json containing and and val
    print('{"angle": %s, "value": %s}' % (ang, val))
    
if __name__=='__main__':
    main()
   	
