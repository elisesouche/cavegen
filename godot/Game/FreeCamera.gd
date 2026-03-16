extends Node3D

# Sensitivity values can be tuned as needed
var mouse_sensitivity := 0.1
var move_speed := 5.0

var yaw := 0.0
var pitch := 0.0

func _ready():
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func _input(event):
	if event is InputEventMouseMotion:
		yaw -= event.relative.x * mouse_sensitivity * 0.01  # scale for radians
		pitch -= event.relative.y * mouse_sensitivity * 0.01  # scale for radians
		pitch = clamp(pitch, -PI/2, PI/2)
	elif event is InputEventKey and event.keycode == KEY_ESCAPE and event.pressed:
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)

func _process(delta):
	var velocity := Vector3.ZERO
	if Input.is_action_pressed("move_forward"):
		velocity -= transform.basis.z
	if Input.is_action_pressed("move_back"):
		velocity += transform.basis.z
	if Input.is_action_pressed("move_left"):
		velocity -= transform.basis.x
	if Input.is_action_pressed("move_right"):
		velocity += transform.basis.x
	if velocity != Vector3.ZERO:
		velocity = velocity.normalized() * move_speed * delta
		transform.origin += velocity

	var rot := Vector3(pitch, yaw, 0)
	transform.basis = Basis.from_euler(rot)
