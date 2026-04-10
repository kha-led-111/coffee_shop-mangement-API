// ═══════════════════════════════════════════════════════════════════════════
// FLUTTER INTEGRATION GUIDE — Coffee Shop API
// ═══════════════════════════════════════════════════════════════════════════
// Add these to pubspec.yaml:
//   dio: ^5.4.0
//   signalr_netcore: ^1.3.4
//   shared_preferences: ^2.2.2
// ═══════════════════════════════════════════════════════════════════════════

import 'package:dio/dio.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:shared_preferences/shared_preferences.dart';

// ─── 1. API Client Setup ──────────────────────────────────────────────────────
class ApiClient {
  static const String baseUrl = 'http://YOUR_SERVER_IP:5000/api';
  static String? _token;

  static Dio get dio {
    final d = Dio(BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
      headers: {
        'Content-Type': 'application/json',
        if (_token != null) 'Authorization': 'Bearer $_token',
      },
    ));

    // Interceptor: auto-attach token
    d.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final prefs = await SharedPreferences.getInstance();
        final token = prefs.getString('jwt_token');
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        return handler.next(options);
      },
      onError: (DioException e, handler) {
        if (e.response?.statusCode == 401) {
          // TODO: redirect to login
        }
        return handler.next(e);
      },
    ));

    return d;
  }

  static void setToken(String token) => _token = token;
}

// ─── 2. Auth Service ──────────────────────────────────────────────────────────
class AuthService {
  Future<Map<String, dynamic>?> login(String email, String password) async {
    try {
      final res = await ApiClient.dio.post('/auth/login', data: {
        'email': email,
        'password': password,
      });
      final data = res.data['data'];
      // Save token
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString('jwt_token', data['token']);
      return data;
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Login failed');
    }
  }
}

// ─── 3. Menu Service ──────────────────────────────────────────────────────────
class MenuService {
  Future<List<dynamic>> getCategories() async {
    final res = await ApiClient.dio.get('/menu/categories');
    return res.data['data'] as List;
  }

  Future<Map<String, dynamic>> getMenuItems({int page = 1, int? categoryId}) async {
    final res = await ApiClient.dio.get('/menu/items', queryParameters: {
      'page': page,
      'pageSize': 20,
      if (categoryId != null) 'categoryId': categoryId,
      'isAvailable': true,
    });
    return res.data['data'];
  }

  // Upload image using multipart
  Future<void> uploadMenuItemImage(int itemId, String filePath) async {
    final formData = FormData.fromMap({
      'image': await MultipartFile.fromFile(filePath, filename: 'item.jpg'),
    });
    await ApiClient.dio.post('/menu/items/$itemId/image', data: formData);
  }
}

// ─── 4. Order Service ─────────────────────────────────────────────────────────
class OrderService {
  Future<Map<String, dynamic>> createOrder({
    required String type,         // 'DineIn' | 'Takeaway'
    int? tableId,
    String? notes,
    required List<Map<String, dynamic>> items,
    //  items: [{ 'menuItemId': 1, 'quantity': 2, 'notes': null }]
  }) async {
    final res = await ApiClient.dio.post('/orders', data: {
      'type': type,
      if (tableId != null) 'tableId': tableId,
      if (notes != null) 'notes': notes,
      'items': items,
    });
    return res.data['data'];
  }

  Future<void> updateOrderStatus(int orderId, String status) async {
    await ApiClient.dio.patch('/orders/$orderId/status', data: {'status': status});
  }

  Future<Map<String, dynamic>> getOrders({int page = 1, String? status}) async {
    final res = await ApiClient.dio.get('/orders', queryParameters: {
      'page': page,
      if (status != null) 'status': status,
    });
    return res.data['data'];
  }
}

// ─── 5. Table Service ─────────────────────────────────────────────────────────
class TableService {
  Future<List<dynamic>> getTables() async {
    final res = await ApiClient.dio.get('/tables');
    return res.data['data'] as List;
  }

  Future<List<dynamic>> getReservations({DateTime? date}) async {
    final res = await ApiClient.dio.get('/tables/reservations', queryParameters: {
      if (date != null) 'date': date.toIso8601String(),
    });
    return res.data['data'] as List;
  }

  Future<Map<String, dynamic>> createReservation({
    required String customerName,
    required String customerPhone,
    required int guestCount,
    required DateTime reservedAt,
    required int tableId,
    String? notes,
  }) async {
    final res = await ApiClient.dio.post('/tables/reservations', data: {
      'customerName':  customerName,
      'customerPhone': customerPhone,
      'guestCount':    guestCount,
      'reservedAt':    reservedAt.toIso8601String(),
      'tableId':       tableId,
      if (notes != null) 'notes': notes,
    });
    return res.data['data'];
  }
}

// ─── 6. SignalR — Live Order Status Updates ───────────────────────────────────
class OrderHubService {
  HubConnection? _connection;

  Future<void> connect(String token) async {
    _connection = HubConnectionBuilder()
      .withUrl(
        'http://YOUR_SERVER_IP:5000/hubs/orders?access_token=$token',
        options: HttpConnectionOptions(
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true,
        ),
      )
      .withAutomaticReconnect()
      .build();

    // Listen for order status changes
    _connection!.on('OrderStatusChanged', (args) {
      if (args != null && args.isNotEmpty) {
        final update = args[0] as Map<String, dynamic>;
        print('Order ${update['orderNumber']} → ${update['statusLabel']}');
        // TODO: dispatch to your state manager (Bloc/Riverpod/Provider)
      }
    });

    // Listen for new orders (kitchen display)
    _connection!.on('NewOrder', (args) {
      if (args != null && args.isNotEmpty) {
        final order = args[0] as Map<String, dynamic>;
        print('New order received: ${order['orderNumber']}');
      }
    });

    await _connection!.start();
  }

  // Subscribe to a specific order's updates
  Future<void> watchOrder(int orderId) async {
    await _connection?.invoke('JoinOrderGroup', args: [orderId]);
  }

  // Staff join kitchen group
  Future<void> joinKitchen() async {
    await _connection?.invoke('JoinKitchen');
  }

  Future<void> disconnect() async {
    await _connection?.stop();
  }
}

// ─── 7. Response Model Example ────────────────────────────────────────────────
// Every endpoint returns:
// {
//   "success": true,
//   "message": "Success",
//   "data": { ... },
//   "errors": []
// }
//
// Usage in Flutter:
//   final res = await ApiClient.dio.get('/menu/items');
//   if (res.data['success'] == true) {
//     final items = res.data['data']['items'] as List;
//   }
