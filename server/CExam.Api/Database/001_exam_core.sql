-- First business migration. The API bootstrap executes the same schema for local demos.
CREATE TABLE IF NOT EXISTS app_users (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(64) NOT NULL UNIQUE,
    password_hash VARCHAR(256) NOT NULL,
    role VARCHAR(32) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS questions (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    owner_id BIGINT NULL,
    stem TEXT NOT NULL,
    option_a VARCHAR(500) NOT NULL,
    option_b VARCHAR(500) NOT NULL,
    option_c VARCHAR(500) NOT NULL,
    option_d VARCHAR(500) NOT NULL,
    correct_option TINYINT NOT NULL,
    points INT NOT NULL DEFAULT 1,
    is_published BOOLEAN NOT NULL DEFAULT TRUE,
    visibility VARCHAR(32) NOT NULL DEFAULT 'PublicPractice'
);

CREATE TABLE IF NOT EXISTS question_answers (
    user_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    selected_option TINYINT NOT NULL,
    is_correct BOOLEAN NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, question_id),
    CONSTRAINT fk_answer_user FOREIGN KEY (user_id) REFERENCES app_users(id),
    CONSTRAINT fk_answer_question FOREIGN KEY (question_id) REFERENCES questions(id)
);

CREATE TABLE IF NOT EXISTS exams (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    created_by BIGINT NOT NULL,
    is_published BOOLEAN NOT NULL DEFAULT TRUE,
    start_at DATETIME NULL,
    end_at DATETIME NULL,
    grades_published BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_exam_creator FOREIGN KEY (created_by) REFERENCES app_users(id)
);

CREATE TABLE IF NOT EXISTS exam_questions (
    exam_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    sort_order INT NOT NULL,
    points INT NOT NULL DEFAULT 1,
    stem TEXT NULL,
    option_a VARCHAR(500) NULL,
    option_b VARCHAR(500) NULL,
    option_c VARCHAR(500) NULL,
    option_d VARCHAR(500) NULL,
    correct_option TINYINT NULL,
    PRIMARY KEY (exam_id, question_id),
    CONSTRAINT fk_exam_question_exam FOREIGN KEY (exam_id) REFERENCES exams(id),
    CONSTRAINT fk_exam_question_question FOREIGN KEY (question_id) REFERENCES questions(id)
);

CREATE TABLE IF NOT EXISTS exam_attempts (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    exam_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    status VARCHAR(32) NOT NULL DEFAULT 'InProgress',
    started_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    submitted_at TIMESTAMP NULL,
    total_score INT NULL,
    UNIQUE KEY uq_exam_attempt_user (exam_id, user_id),
    CONSTRAINT fk_attempt_exam FOREIGN KEY (exam_id) REFERENCES exams(id),
    CONSTRAINT fk_attempt_user FOREIGN KEY (user_id) REFERENCES app_users(id)
);

CREATE TABLE IF NOT EXISTS exam_answers (
    attempt_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    selected_option TINYINT NOT NULL,
    is_correct BOOLEAN NOT NULL,
    points INT NOT NULL DEFAULT 0,
    revision BIGINT NOT NULL DEFAULT 0,
    PRIMARY KEY (attempt_id, question_id),
    CONSTRAINT fk_exam_answer_attempt FOREIGN KEY (attempt_id) REFERENCES exam_attempts(id),
    CONSTRAINT fk_exam_answer_question FOREIGN KEY (question_id) REFERENCES questions(id)
);

CREATE TABLE IF NOT EXISTS exam_grade_overrides (
    attempt_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    points INT NOT NULL,
    PRIMARY KEY (attempt_id, question_id),
    CONSTRAINT fk_grade_override_attempt FOREIGN KEY (attempt_id) REFERENCES exam_attempts(id),
    CONSTRAINT fk_grade_override_question FOREIGN KEY (question_id) REFERENCES questions(id)
);

CREATE TABLE IF NOT EXISTS exam_assignments (
    exam_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    PRIMARY KEY (exam_id, user_id),
    CONSTRAINT fk_exam_assignment_exam FOREIGN KEY (exam_id) REFERENCES exams(id),
    CONSTRAINT fk_exam_assignment_user FOREIGN KEY (user_id) REFERENCES app_users(id)
);

CREATE TABLE IF NOT EXISTS classes (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(120) NOT NULL,
    join_code VARCHAR(32) NOT NULL UNIQUE,
    teacher_id BIGINT NOT NULL,
    is_open BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_class_teacher FOREIGN KEY (teacher_id) REFERENCES app_users(id)
);

CREATE TABLE IF NOT EXISTS class_members (
    class_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (class_id, user_id),
    CONSTRAINT fk_class_member_class FOREIGN KEY (class_id) REFERENCES classes(id),
    CONSTRAINT fk_class_member_user FOREIGN KEY (user_id) REFERENCES app_users(id)
);
