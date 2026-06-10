module FSMC (
    input  wire        CLK_20M,    // Clock h? th?ng 20MHz
    inout  wire [15:0] DATA,       // Bus d? li?u FSMC
    input  wire        NWE,        // Xung Ghi (Write)
    input  wire        NOE,        // Xung Ð?c (Read)
    input  wire        NADV,       // Xung ch?t d?a ch?
    input  wire        NE1,        // Chip Select Bank 1
    input  wire        NC_PIN_37, NC_PIN_39, NC_PIN_88, NC_PIN_90,

    // Tín hi?u giao ti?p TB6612FNG (Ð?ng co)
    output reg         PULSE1,     // PWMA
    output wire        IN1,        // AIN1
    output wire        IN2,        // AIN2

    // Tín hi?u giao ti?p Encoder Quang
    input  wire        ENC_A,      // Kênh A t? d?ng co
    input  wire        ENC_B       // Kênh B t? d?ng co
);

    parameter PWM_PERIOD = 1000; // PWM 20kHz

    // --- CÁC THANH GHI N?I B? ---
    reg [15:0] addr_reg = 16'd0;
    
    // Ð?ng co
    reg [14:0] pwm_duty = 15'd0;
    reg        motor_dir = 1'b0;
    reg [15:0] pwm_cnt = 16'd0;

    // Encoder
    reg signed [15:0] encoder_count = 16'd0;
    reg [2:0] enc_a_sync, enc_b_sync; // Thanh ghi d?ng b? ch?ng nhi?u
    wire a_rise, a_fall, b_rise, b_fall;

    // --- 1. GIAO TI?P FSMC (Ð?C / GHI) ---
    
    // Ch?t d?a ch?
    always @(posedge NADV) begin
        if (!NE1) addr_reg <= DATA;
    end

    // STM32 Ghi l?nh di?u khi?n d?ng co
    always @(posedge NWE) begin
        if (!NE1 && addr_reg == 16'h0000) begin
            motor_dir <= DATA[15];
            pwm_duty  <= DATA[14:0];
        end
    end

    // STM32 Ð?c giá tr? Encoder (Tr? kháng cao 'bz' n?u không du?c ch?n)
    assign DATA = (!NE1 && !NOE && addr_reg == 16'h0001) ? encoder_count : 16'bz;

    // --- 2. B? PHÁT PWM 20KHz ---
    always @(posedge CLK_20M) begin
        if (pwm_cnt < PWM_PERIOD - 1)
            pwm_cnt <= pwm_cnt + 1'b1;
        else
            pwm_cnt <= 16'd0;

        PULSE1 <= (pwm_cnt < pwm_duty) ? 1'b1 : 1'b0;
    end
    
    assign IN1 = motor_dir;
    assign IN2 = ~motor_dir;

    // --- 3. B? Ð?C ENCODER x4 (QUADRATURE DECODER) ---
    // Ð?ng b? hóa tín hi?u t? ngoài vào domain clock 20MHz
    always @(posedge CLK_20M) begin
        enc_a_sync <= {enc_a_sync[1:0], ENC_A};
        enc_b_sync <= {enc_b_sync[1:0], ENC_B};
    end

    // B?t su?n c?nh (Edge Detection)
    assign a_rise = (enc_a_sync[2:1] == 2'b01);
    assign a_fall = (enc_a_sync[2:1] == 2'b10);
    assign b_rise = (enc_b_sync[2:1] == 2'b01);
    assign b_fall = (enc_b_sync[2:1] == 2'b10);

    // Thu?t toán d?m x4: C?p nh?t v? trí trên m?i su?n lên/xu?ng c?a c? A và B
    always @(posedge CLK_20M) begin
        if (a_rise) begin
            if (enc_b_sync[1]) encoder_count <= encoder_count - 1'b1;
            else               encoder_count <= encoder_count + 1'b1;
        end
        else if (a_fall) begin
            if (enc_b_sync[1]) encoder_count <= encoder_count + 1'b1;
            else               encoder_count <= encoder_count - 1'b1;
        end
        else if (b_rise) begin
            if (enc_a_sync[1]) encoder_count <= encoder_count + 1'b1;
            else               encoder_count <= encoder_count - 1'b1;
        end
        else if (b_fall) begin
            if (enc_a_sync[1]) encoder_count <= encoder_count - 1'b1;
            else               encoder_count <= encoder_count + 1'b1;
        end
    end

endmodule
